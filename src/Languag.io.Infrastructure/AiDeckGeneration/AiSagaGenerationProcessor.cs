using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.AiSagaGeneration;
using Languag.io.Application.Audio;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class AiSagaGenerationProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IAiSagaGenerator _aiSagaGenerator;
    private readonly IAudioAssetService? _audioAssetService;
    private readonly ILogger<AiSagaGenerationProcessor> _logger;

    public AiSagaGenerationProcessor(
        AppDbContext dbContext,
        IAiSagaGenerator aiSagaGenerator,
        ILogger<AiSagaGenerationProcessor> logger)
        : this(dbContext, aiSagaGenerator, null, logger)
    {
    }

    public AiSagaGenerationProcessor(
        AppDbContext dbContext,
        IAiSagaGenerator aiSagaGenerator,
        IAudioAssetService? audioAssetService,
        ILogger<AiSagaGenerationProcessor> logger)
    {
        _dbContext = dbContext;
        _aiSagaGenerator = aiSagaGenerator;
        _audioAssetService = audioAssetService;
        _logger = logger;
    }

    public async Task<bool> ProcessNextPendingJobAsync(CancellationToken ct = default)
    {
        var job = await ClaimNextPendingJobAsync(ct);
        if (job is null)
        {
            return false;
        }

        try
        {
            var generatedSaga = await _aiSagaGenerator.GenerateSagaAsync(job, ct);
            ValidateGeneratedSaga(generatedSaga, job.RequestedDeckCount, job.RequestedCardsPerDeck, job.RequestedMultiChoiceCountPerDeck);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            var now = DateTime.UtcNow;
            var sagaDeckAudioTextByDeckId = new Dictionary<Guid, IReadOnlyDictionary<int, string?>>();
            var activityState = new DeckActivityState
            {
                IsFirstDeck = !await _dbContext.Decks
                    .AsNoTracking()
                    .AnyAsync(deck => deck.OwnerId == job.UserId, ct)
            };

            var saga = new Saga
            {
                Id = Guid.NewGuid(),
                OwnerId = job.UserId,
                Title = generatedSaga.Title.Trim(),
                Description = TrimToNull(generatedSaga.Description),
                Category = string.IsNullOrWhiteSpace(generatedSaga.Category)
                    ? "Languages"
                    : generatedSaga.Category.Trim(),
                Color = "teal",
                Visibility = DeckVisibility.Private,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            saga.Chapters = generatedSaga.Chapters
                .Select((chapter, chapterIndex) => CreateChapter(
                    chapter,
                    chapterIndex,
                    job,
                    saga,
                    now,
                    sagaDeckAudioTextByDeckId,
                    activityState))
                .ToList();

            _dbContext.Sagas.Add(saga);

            job.Status = AiSagaGenerationStatus.Completed;
            job.AudioStatus = job.IncludeAudio
                ? AiSagaAudioStatus.Processing
                : AiSagaAudioStatus.NotRequested;
            job.CreatedSagaId = saga.Id;
            job.CompletedAtUtc = now;

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            if (job.IncludeAudio)
            {
                await ProcessSagaAudioAsync(job.Id, sagaDeckAudioTextByDeckId, job.TargetLanguage, ct);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI saga generation job {JobId} failed.", job.Id);

            _dbContext.ChangeTracker.Clear();

            var failedJob = await _dbContext.AiSagaGenerationJobs
                .FirstOrDefaultAsync(candidate => candidate.Id == job.Id, ct);

            if (failedJob is null)
            {
                return true;
            }

            failedJob.Status = AiSagaGenerationStatus.Failed;
            failedJob.RetryCount += 1;
            failedJob.ErrorMessage = ex.Message;
            failedJob.CompletedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            return true;
        }
    }

    private async Task<AiSagaGenerationJob?> ClaimNextPendingJobAsync(CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        var job = await ReadNextPendingJobForClaimAsync(ct);

        if (job is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        job.Status = AiSagaGenerationStatus.Processing;
        job.StartedAtUtc = DateTime.UtcNow;
        job.ErrorMessage = null;

        await _dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return job;
    }

    private Task<AiSagaGenerationJob?> ReadNextPendingJobForClaimAsync(CancellationToken ct)
    {
        if (string.Equals(
            _dbContext.Database.ProviderName,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal))
        {
            return _dbContext.AiSagaGenerationJobs
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "AiSagaGenerationJobs"
                    WHERE "Id" = (
                        SELECT "Id"
                        FROM "AiSagaGenerationJobs"
                        WHERE "Status" = {(int)AiSagaGenerationStatus.Pending}
                        ORDER BY "CreatedAtUtc"
                        FOR UPDATE SKIP LOCKED
                        LIMIT 1
                    )
                    """)
                .FirstOrDefaultAsync(ct);
        }

        return _dbContext.AiSagaGenerationJobs
            .Where(candidate => candidate.Status == AiSagaGenerationStatus.Pending)
            .OrderBy(candidate => candidate.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    private SagaChapter CreateChapter(
        GeneratedSagaChapterDto source,
        int chapterIndex,
        AiSagaGenerationJob job,
        Saga saga,
        DateTime now,
        IDictionary<Guid, IReadOnlyDictionary<int, string?>> sagaDeckAudioTextByDeckId,
        DeckActivityState activityState)
    {
        var chapter = new SagaChapter
        {
            Id = Guid.NewGuid(),
            SagaId = saga.Id,
            Title = source.Title.Trim(),
            Description = TrimToNull(source.Description),
            Order = chapterIndex
        };

        chapter.Lessons = source.Lessons
            .Select((lesson, lessonIndex) => CreateLesson(
                lesson,
                lessonIndex,
                job,
                saga.Category ?? "Languages",
                chapter,
                now,
                sagaDeckAudioTextByDeckId,
                activityState))
            .ToList();

        return chapter;
    }

    private SagaLesson CreateLesson(
        GeneratedSagaLessonDto source,
        int lessonIndex,
        AiSagaGenerationJob job,
        string sagaCategory,
        SagaChapter chapter,
        DateTime now,
        IDictionary<Guid, IReadOnlyDictionary<int, string?>> sagaDeckAudioTextByDeckId,
        DeckActivityState activityState)
    {
        var deck = CreateDeck(source.Deck, job.UserId, sagaCategory, now);
        sagaDeckAudioTextByDeckId[deck.Id] = source.Deck.Cards
            .Select((card, index) => new { index, card.TtsText })
            .ToDictionary(item => item.index, item => item.TtsText);

        _dbContext.Decks.Add(deck);
        _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.DeckCreated, now));

        if (activityState.IsFirstDeck && !activityState.FirstDeckLogWritten)
        {
            _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.FirstDeckCreated, now));
            activityState.FirstDeckLogWritten = true;
            activityState.IsFirstDeck = false;
        }

        return new SagaLesson
        {
            Id = Guid.NewGuid(),
            SagaChapterId = chapter.Id,
            DeckId = deck.Id,
            Deck = deck,
            Title = TrimToNull(source.Title),
            Description = TrimToNull(source.Description),
            Order = lessonIndex
        };
    }

    private static Deck CreateDeck(
        GeneratedDeckDto source,
        Guid ownerId,
        string sagaCategory,
        DateTime now)
    {
        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = source.Title.Trim(),
            Description = TrimToNull(source.Description),
            Category = string.IsNullOrWhiteSpace(source.Category)
                ? sagaCategory
                : source.Category.Trim(),
            Color = "teal",
            Visibility = DeckVisibility.Private,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Cards = source.Cards
                .Select((card, index) => CreateCard(card, index, now))
                .ToList()
        };

        foreach (var card in deck.Cards)
        {
            card.DeckId = deck.Id;
        }

        return deck;
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
            ExampleSentence = TrimToNull(source.ExampleSentence),
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

    private async Task ProcessSagaAudioAsync(
        Guid jobId,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<int, string?>> audioTextByDeckId,
        string? targetLanguage,
        CancellationToken ct)
    {
        if (_audioAssetService is null)
        {
            await MarkAudioStatusAsync(jobId, AiSagaAudioStatus.Failed, ct);
            return;
        }

        var languageCode = string.IsNullOrWhiteSpace(targetLanguage)
            ? "target-language"
            : targetLanguage.Trim();

        var attachedReadyAssetCount = 0;

        foreach (var (deckId, audioTextByOrder) in audioTextByDeckId)
        {
            var cards = await _dbContext.Cards
                .Where(card => card.DeckId == deckId && card.Type == CardTypes.Flashcard)
                .OrderBy(card => card.Order)
                .ToListAsync(ct);

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
        }

        await _dbContext.SaveChangesAsync(ct);
        await MarkAudioStatusAsync(
            jobId,
            attachedReadyAssetCount > 0 ? AiSagaAudioStatus.Ready : AiSagaAudioStatus.Failed,
            ct);
    }

    private async Task MarkAudioStatusAsync(
        Guid jobId,
        AiSagaAudioStatus audioStatus,
        CancellationToken ct)
    {
        var job = await _dbContext.AiSagaGenerationJobs
            .FirstOrDefaultAsync(candidate => candidate.Id == jobId, ct);

        if (job is null)
        {
            return;
        }

        job.AudioStatus = audioStatus;
        await _dbContext.SaveChangesAsync(ct);
    }

    private static void ValidateGeneratedSaga(
        GeneratedSagaDto saga,
        int requestedDeckCount,
        int requestedCardsPerDeck,
        int requestedMultiChoiceCountPerDeck)
    {
        if (string.IsNullOrWhiteSpace(saga.Title))
        {
            throw new InvalidOperationException("Generated saga title is empty.");
        }

        if (saga.Chapters.Count == 0)
        {
            throw new InvalidOperationException("Generated saga has no chapters.");
        }

        var lessons = saga.Chapters.SelectMany(chapter => chapter.Lessons).ToList();
        if (lessons.Count != requestedDeckCount)
        {
            throw new InvalidOperationException("Generated saga deck count does not match the requested deck count.");
        }

        foreach (var chapter in saga.Chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Title))
            {
                throw new InvalidOperationException("Generated saga chapter title is empty.");
            }

            if (chapter.Lessons.Count == 0)
            {
                throw new InvalidOperationException("Generated saga chapter has no lessons.");
            }
        }

        foreach (var lesson in lessons)
        {
            ValidateGeneratedDeck(
                lesson.Deck,
                requestedCardsPerDeck,
                requestedMultiChoiceCountPerDeck);
        }
    }

    private static void ValidateGeneratedDeck(
        GeneratedDeckDto deck,
        int requestedCardsPerDeck,
        int requestedMultiChoiceCountPerDeck)
    {
        if (string.IsNullOrWhiteSpace(deck.Title))
        {
            throw new InvalidOperationException("Generated saga deck title is empty.");
        }

        if (deck.Cards.Count != requestedCardsPerDeck)
        {
            throw new InvalidOperationException("Generated saga deck card count does not match the requested card count.");
        }

        var multiChoiceCount = 0;

        foreach (var card in deck.Cards)
        {
            var type = CardTypes.Normalize(card.Type);
            if (!CardTypes.IsSupported(type))
            {
                throw new InvalidOperationException("Generated saga card has an unsupported type.");
            }

            if (string.IsNullOrWhiteSpace(card.FrontText))
            {
                throw new InvalidOperationException("Generated saga card has empty front text.");
            }

            if (string.IsNullOrWhiteSpace(card.BackText))
            {
                throw new InvalidOperationException("Generated saga card has empty back text.");
            }

            if (type == CardTypes.MultiChoice)
            {
                multiChoiceCount += 1;
                ValidateGeneratedChoices(card);
            }
        }

        if (multiChoiceCount != requestedMultiChoiceCountPerDeck)
        {
            throw new InvalidOperationException("Generated saga multi-choice card count does not match the requested count.");
        }
    }

    private static void ValidateGeneratedChoices(GeneratedCardDto card)
    {
        var choices = card.Choices
            .Where(choice => !string.IsNullOrWhiteSpace(choice.Text))
            .ToList();

        if (choices.Count < 2)
        {
            throw new InvalidOperationException("Generated saga multi-choice card has too few choices.");
        }

        if (choices.Count(choice => choice.IsCorrect) != 1)
        {
            throw new InvalidOperationException("Generated saga multi-choice card must have exactly one correct choice.");
        }
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class DeckActivityState
    {
        public bool IsFirstDeck { get; set; }
        public bool FirstDeckLogWritten { get; set; }
    }
}
