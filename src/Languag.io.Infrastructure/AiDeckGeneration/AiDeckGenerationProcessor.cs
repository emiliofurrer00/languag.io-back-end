using Languag.io.Application.AiDeckGeneration;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class AiDeckGenerationProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly IAiDeckGenerator _aiDeckGenerator;
    private readonly ILogger<AiDeckGenerationProcessor> _logger;

    public AiDeckGenerationProcessor(
        AppDbContext dbContext,
        IAiDeckGenerator aiDeckGenerator,
        ILogger<AiDeckGenerationProcessor> logger)
    {
        _dbContext = dbContext;
        _aiDeckGenerator = aiDeckGenerator;
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
            ValidateGeneratedDeck(generatedDeck, job.RequestedCardCount);

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
                    .Select((card, index) => new Card
                    {
                        Id = Guid.NewGuid(),
                        Type = CardTypes.Flashcard,
                        FrontText = card.FrontText.Trim(),
                        BackText = card.BackText.Trim(),
                        ExampleSentence = string.IsNullOrWhiteSpace(card.ExampleSentence)
                            ? null
                            : card.ExampleSentence.Trim(),
                        Order = index,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    })
                    .ToList()
            };

            _dbContext.Decks.Add(deck);
            _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.DeckCreated, now));

            if (isFirstDeck)
            {
                _dbContext.ActivityLogs.Add(CreateActivityLog(job.UserId, deck.Id, ActivityType.FirstDeckCreated, now));
            }

            job.Status = AiDeckGenerationStatus.Completed;
            job.CreatedDeckId = deck.Id;
            job.CompletedAtUtc = now;

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

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

    private static void ValidateGeneratedDeck(GeneratedDeckDto deck, int requestedCardCount)
    {
        if (string.IsNullOrWhiteSpace(deck.Title))
        {
            throw new InvalidOperationException("Generated deck title is empty.");
        }

        if (deck.Cards.Count == 0)
        {
            throw new InvalidOperationException("Generated deck has no cards.");
        }

        if (deck.Cards.Count > requestedCardCount + 5)
        {
            throw new InvalidOperationException("Generated too many cards.");
        }

        foreach (var card in deck.Cards)
        {
            if (string.IsNullOrWhiteSpace(card.FrontText))
            {
                throw new InvalidOperationException("Generated card has empty front text.");
            }

            if (string.IsNullOrWhiteSpace(card.BackText))
            {
                throw new InvalidOperationException("Generated card has empty back text.");
            }
        }
    }
}
