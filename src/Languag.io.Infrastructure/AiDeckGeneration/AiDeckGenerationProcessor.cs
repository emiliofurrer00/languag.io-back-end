using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.Audio;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class AiDeckGenerationProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IAiDeckGenerator _aiDeckGenerator;
    private readonly IAudioAssetService? _audioAssetService;
    private readonly ILogger<AiDeckGenerationProcessor> _logger;

    public AiDeckGenerationProcessor(
        AppDbContext dbContext,
        IAiDeckGenerator aiDeckGenerator,
        ILogger<AiDeckGenerationProcessor> logger)
        : this(dbContext, aiDeckGenerator, null, logger)
    {
    }

    public AiDeckGenerationProcessor(
        AppDbContext dbContext,
        IAiDeckGenerator aiDeckGenerator,
        IAudioAssetService? audioAssetService,
        ILogger<AiDeckGenerationProcessor> logger)
    {
        _dbContext = dbContext;
        _aiDeckGenerator = aiDeckGenerator;
        _audioAssetService = audioAssetService;
        _logger = logger;
    }

    public async Task<bool> ProcessNextPendingJobAsync(CancellationToken ct = default)
    {
        var job = await _dbContext.AiDeckGenerationJobs
            .Where(candidate => candidate.Status == AiDeckGenerationStatus.Pending)
            .OrderBy(candidate => candidate.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (job is null)
        {
            return false;
        }

        job.Status = AiDeckGenerationStatus.Processing;
        job.StartedAtUtc = DateTime.UtcNow;
        job.ErrorMessage = null;

        await _dbContext.SaveChangesAsync(ct);

        try
        {
            var generatedDeck = await _aiDeckGenerator.GenerateDeckAsync(job, ct);
            ValidateGeneratedDeck(generatedDeck, job.RequestedCardCount, job.RequestedMultiChoiceCount);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            var now = DateTime.UtcNow;
            var isFirstDeck = !await _dbContext.Decks
                .AsNoTracking()
                .AnyAsync(deck => deck.OwnerId == job.UserId, ct);

            var deck = new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = job.UserId,
                Title = generatedDeck.Title.Trim(),
                Description = generatedDeck.Description?.Trim(),
                Category = string.IsNullOrWhiteSpace(generatedDeck.Category)
                    ? "Languages"
                    : generatedDeck.Category.Trim(),
                Color = "teal",
                Visibility = DeckVisibility.Private,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Cards = generatedDeck.Cards
                    .Select((card, index) => CreateCard(card, index, now))
                    .ToList()
            };

            _dbContext.Decks.Add(deck);
            _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.DeckCreated, now));

            if (isFirstDeck)
            {
                _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.FirstDeckCreated, now));
            }

            job.Status = AiDeckGenerationStatus.Completed;
            job.AudioStatus = job.IncludeAudio
                ? AiDeckAudioStatus.Processing
                : AiDeckAudioStatus.NotRequested;
            job.CreatedDeckId = deck.Id;
            job.CompletedAtUtc = now;

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            if (job.IncludeAudio)
            {
                var audioTextByOrder = generatedDeck.Cards
                    .Select((card, index) => new { index, card.TtsText })
                    .ToDictionary(item => item.index, item => item.TtsText);

                await ProcessDeckAudioAsync(job.Id, deck.Id, job.TargetLanguage, audioTextByOrder, ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI deck generation job {JobId} failed.", job.Id);

            _dbContext.ChangeTracker.Clear();

            var failedJob = await _dbContext.AiDeckGenerationJobs
                .FirstOrDefaultAsync(candidate => candidate.Id == job.Id, ct);

            if (failedJob is null)
            {
                return true;
            }

            failedJob.Status = AiDeckGenerationStatus.Failed;
            failedJob.RetryCount += 1;
            failedJob.ErrorMessage = ex.Message;
            failedJob.CompletedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            return true;
        }
    }

    private static ActivityLog CreateActivityLog(
        Guid userId,
        Guid deckId,
        ActivityType activityType,
        DateTime now)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeckId = deckId,
            Type = activityType,
            OccurredAtUtc = now,
            CreatedAtUtc = now
        };
    }

    private async Task ProcessDeckAudioAsync(
        Guid jobId,
        Guid deckId,
        string? targetLanguage,
        IReadOnlyDictionary<int, string?> audioTextByOrder,
        CancellationToken ct)
    {
        if (_audioAssetService is null)
        {
            await MarkAudioStatusAsync(jobId, AiDeckAudioStatus.Failed, ct);
            return;
        }

        var languageCode = string.IsNullOrWhiteSpace(targetLanguage)
            ? "target-language"
            : targetLanguage.Trim();

        var cards = await _dbContext.Cards
            .Where(card => card.DeckId == deckId && card.Type == CardTypes.Flashcard)
            .OrderBy(card => card.Order)
            .ToListAsync(ct);

        var attachedReadyAssetCount = 0;

        foreach (var card in cards)
        {
            var audioText = audioTextByOrder.GetValueOrDefault(card.Order);
            var audioAsset = await _audioAssetService.GetOrCreateAudioAsync(
                string.IsNullOrWhiteSpace(audioText) ? card.FrontText : audioText,
                languageCode,
                ct);

            if (audioAsset is null)
            {
                continue;
            }

            card.FrontAudioAssetId = audioAsset.Id;

            if (audioAsset.Status == AudioAssetStatus.Ready)
            {
                attachedReadyAssetCount += 1;
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        await MarkAudioStatusAsync(
            jobId,
            attachedReadyAssetCount > 0 ? AiDeckAudioStatus.Ready : AiDeckAudioStatus.Failed,
            ct);
    }

    private async Task MarkAudioStatusAsync(
        Guid jobId,
        AiDeckAudioStatus audioStatus,
        CancellationToken ct)
    {
        var job = await _dbContext.AiDeckGenerationJobs
            .FirstOrDefaultAsync(candidate => candidate.Id == jobId, ct);

        if (job is null)
        {
            return;
        }

        job.AudioStatus = audioStatus;
        await _dbContext.SaveChangesAsync(ct);
    }

    private static Card CreateCard(GeneratedCardDto source, int order, DateTime now)
    {
        var id = Guid.NewGuid();
        var type = CardTypes.Normalize(source.Type);

        return new Card
        {
            Id = id,
            Type = type,
            FrontText = source.FrontText.Trim(),
            BackText = source.BackText.Trim(),
            ExampleSentence = string.IsNullOrWhiteSpace(source.ExampleSentence)
                ? null
                : source.ExampleSentence.Trim(),
            Order = order,
            Choices = type == CardTypes.MultiChoice
                ? source.Choices
                    .Where(choice => !string.IsNullOrWhiteSpace(choice.Text))
                    .Select((choice, index) => new CardChoice
                    {
                        Id = Guid.NewGuid(),
                        CardId = id,
                        Text = choice.Text.Trim(),
                        IsCorrect = choice.IsCorrect,
                        Order = index
                    })
                    .ToList()
                : [],
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static void ValidateGeneratedDeck(
        GeneratedDeckDto deck,
        int requestedCardCount,
        int requestedMultiChoiceCount)
    {
        if (string.IsNullOrWhiteSpace(deck.Title))
        {
            throw new InvalidOperationException("Generated deck title is empty.");
        }

        if (deck.Cards.Count != requestedCardCount)
        {
            throw new InvalidOperationException("Generated deck card count does not match the requested card count.");
        }

        var multiChoiceCount = 0;

        foreach (var card in deck.Cards)
        {
            var type = CardTypes.Normalize(card.Type);
            if (!CardTypes.IsSupported(type))
            {
                throw new InvalidOperationException("Generated card has an unsupported type.");
            }

            if (string.IsNullOrWhiteSpace(card.FrontText))
            {
                throw new InvalidOperationException("Generated card has empty front text.");
            }

            if (string.IsNullOrWhiteSpace(card.BackText))
            {
                throw new InvalidOperationException("Generated card has empty back text.");
            }

            if (type == CardTypes.MultiChoice)
            {
                multiChoiceCount += 1;
                ValidateGeneratedChoices(card);
            }
        }

        if (multiChoiceCount != requestedMultiChoiceCount)
        {
            throw new InvalidOperationException("Generated multi-choice card count does not match the requested count.");
        }
    }

    private static void ValidateGeneratedChoices(GeneratedCardDto card)
    {
        var choices = card.Choices
            .Where(choice => !string.IsNullOrWhiteSpace(choice.Text))
            .ToList();

        if (choices.Count < 2)
        {
            throw new InvalidOperationException("Generated multi-choice card has too few choices.");
        }

        if (choices.Count(choice => choice.IsCorrect) != 1)
        {
            throw new InvalidOperationException("Generated multi-choice card must have exactly one correct choice.");
        }
    }
}
