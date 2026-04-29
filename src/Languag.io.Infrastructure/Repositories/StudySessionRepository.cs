using Languag.io.Application.StudySessions;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class StudySessionRepository : IStudySessionRepository
{
    private readonly AppDbContext _dbContext;

    public StudySessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CanAccessDeckAsync(Guid deckId, Guid userId, CancellationToken ct = default)
    {
        return _dbContext.Decks
            .AsNoTracking()
            .AnyAsync(
                deck => deck.Id == deckId
                    && (deck.OwnerId == userId || deck.Visibility == DeckVisibility.Public),
                ct);
    }

    public async Task<bool> DeckContainsCardsAsync(
        Guid deckId,
        IReadOnlyCollection<Guid> cardIds,
        CancellationToken ct = default)
    {
        var distinctCardIds = cardIds.Distinct().ToList();
        if (distinctCardIds.Count == 0)
        {
            return false;
        }

        var matchingCardCount = await _dbContext.Cards
            .AsNoTracking()
            .Where(card => card.DeckId == deckId && distinctCardIds.Contains(card.Id))
            .Select(card => card.Id)
            .Distinct()
            .CountAsync(ct);

        return matchingCardCount == distinctCardIds.Count;
    }

    public Task<bool> UserHasStudySessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return _dbContext.StudySessions
            .AsNoTracking()
            .AnyAsync(studySession => studySession.UserId == userId, ct);
    }

    public async Task<IReadOnlyList<CardReviewState>> GetReviewStatesAsync(
        Guid userId,
        Guid deckId,
        IReadOnlyCollection<Guid> cardIds,
        CancellationToken ct = default)
    {
        var requestedCardIds = cardIds.ToList();

        return await _dbContext.CardReviewStates
            .Where(state => state.UserId == userId &&
                state.DeckId == deckId &&
                requestedCardIds.Contains(state.CardId))
            .ToListAsync(ct);
    }

    public async Task AddReviewStatesAsync(
        IReadOnlyCollection<CardReviewState> reviewStates,
        CancellationToken ct = default)
    {
        await _dbContext.CardReviewStates.AddRangeAsync(reviewStates, ct);
    }

    public async Task<IReadOnlyList<StudyPlanCardDto>?> GetDeckStudyPlanAsync(
        Guid deckId,
        Guid userId,
        DateTime now,
        int limit,
        CancellationToken ct = default)
    {
        var cards = await _dbContext.Cards
            .AsNoTracking()
            .Where(card => card.DeckId == deckId)
            .OrderBy(card => card.Order)
            .Select(card => new StudyPlanCard(
                card.Id,
                card.DeckId,
                card.Type,
                card.FrontText,
                card.BackText,
                card.ExampleSentence,
                card.Choices
                    .OrderBy(choice => choice.Order)
                    .Select(choice => new CardChoiceDto(choice.Id, choice.Text, choice.IsCorrect, choice.Order))
                    .ToList(),
                card.Order))
            .ToListAsync(ct);

        var cardIds = cards.Select(card => card.Id).ToList();
        var states = await _dbContext.CardReviewStates
            .AsNoTracking()
            .Where(state => state.UserId == userId &&
                state.DeckId == deckId &&
                cardIds.Contains(state.CardId))
            .ToDictionaryAsync(state => state.CardId, ct);

        return cards
            .Select(card => MapStudyPlanCard(card, states.GetValueOrDefault(card.Id), now))
            .OrderBy(card => GetStudyPlanPriority(card))
            .ThenBy(card => card.DueAtUtc ?? DateTime.MaxValue)
            .ThenBy(card => card.Order)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<DeckStudyRecommendationDto>> GetStudyRecommendationsAsync(
        Guid userId,
        DateTime now,
        int limit,
        CancellationToken ct = default)
    {
        var decks = await _dbContext.Decks
            .AsNoTracking()
            .Include(deck => deck.Cards)
            .Where(deck => deck.OwnerId == userId || deck.Visibility == DeckVisibility.Public)
            .ToListAsync(ct);

        var deckIds = decks.Select(deck => deck.Id).ToList();
        var states = await _dbContext.CardReviewStates
            .AsNoTracking()
            .Where(state => state.UserId == userId && deckIds.Contains(state.DeckId))
            .ToListAsync(ct);

        var statesByDeckId = states
            .GroupBy(state => state.DeckId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return decks
            .Select(deck => MapRecommendation(deck, statesByDeckId.GetValueOrDefault(deck.Id) ?? [], now))
            .Where(recommendation => recommendation.TotalCards > 0)
            .OrderByDescending(recommendation => recommendation.PriorityScore)
            .ThenBy(recommendation => recommendation.NextDueAtUtc ?? DateTime.MaxValue)
            .ThenBy(recommendation => recommendation.Title)
            .Take(limit)
            .ToList();
    }

    public async Task AddAsync(StudySession studySession, CancellationToken ct = default)
    {
        await _dbContext.StudySessions.AddAsync(studySession, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    private static StudyPlanCardDto MapStudyPlanCard(
        StudyPlanCard card,
        CardReviewState? state,
        DateTime now)
    {
        var isNew = state is null;
        var isDue = state?.DueAtUtc <= now;
        var reason = GetStudyPlanReason(state, now);

        return new StudyPlanCardDto(
            card.Id,
            card.DeckId,
            card.Type,
            card.FrontText,
            card.BackText,
            card.ExampleSentence,
            card.Choices,
            card.Order,
            isNew,
            isDue,
            state?.LastReviewedAtUtc,
            state?.DueAtUtc,
            state?.IntervalDays ?? 0,
            state?.EaseFactor ?? 2.5m,
            state?.RepetitionCount ?? 0,
            state?.LapseCount ?? 0,
            state?.TotalReviews ?? 0,
            state?.CorrectReviews ?? 0,
            CalculateAccuracy(state),
            reason);
    }

    private static int GetStudyPlanPriority(StudyPlanCardDto card)
    {
        if (card.IsDue)
        {
            return 0;
        }

        if (card.Reason == StudyPlanReasons.Lapsed)
        {
            return 1;
        }

        if (card.IsNew)
        {
            return 2;
        }

        return 3;
    }

    private static string GetStudyPlanReason(CardReviewState? state, DateTime now)
    {
        if (state is null)
        {
            return StudyPlanReasons.New;
        }

        if (state.DueAtUtc <= now)
        {
            return StudyPlanReasons.Due;
        }

        if (state.LapseCount > 0 && state.RepetitionCount == 0)
        {
            return StudyPlanReasons.Lapsed;
        }

        return StudyPlanReasons.Review;
    }

    private static DeckStudyRecommendationDto MapRecommendation(
        Deck deck,
        IReadOnlyCollection<CardReviewState> states,
        DateTime now)
    {
        var cardIds = deck.Cards.Select(card => card.Id).ToHashSet();
        var deckStates = states
            .Where(state => cardIds.Contains(state.CardId))
            .ToArray();
        var reviewedCardIds = deckStates.Select(state => state.CardId).ToHashSet();
        var totalCards = deck.Cards.Count;
        var newCards = totalCards - reviewedCardIds.Count;
        var dueCards = deckStates.Count(state => state.DueAtUtc <= now);
        var overdueCards = deckStates.Count(state => state.DueAtUtc <= now.AddDays(-1));
        var lapsedCards = deckStates.Count(state => state.LapseCount > 0 && state.RepetitionCount == 0);
        var nextDueAtUtc = deckStates
            .OrderBy(state => state.DueAtUtc)
            .Select(state => (DateTime?)state.DueAtUtc)
            .FirstOrDefault();
        var totalReviews = deckStates.Sum(state => state.TotalReviews);
        decimal? accuracy = totalReviews == 0
            ? null
            : decimal.Round(deckStates.Sum(state => state.CorrectReviews) * 100m / totalReviews, 2);
        var lastReviewedAtUtc = deckStates
            .Select(state => state.LastReviewedAtUtc)
            .Where(lastReviewedAt => lastReviewedAt.HasValue)
            .DefaultIfEmpty()
            .Max();
        var priorityScore = CalculatePriorityScore(
            dueCards,
            overdueCards,
            newCards,
            lapsedCards,
            accuracy,
            lastReviewedAtUtc,
            now);

        return new DeckStudyRecommendationDto(
            deck.Id,
            deck.Title,
            deck.Category ?? string.Empty,
            deck.Description,
            deck.Color,
            totalCards,
            dueCards,
            newCards,
            lapsedCards,
            overdueCards,
            nextDueAtUtc,
            accuracy,
            priorityScore);
    }

    private static decimal CalculatePriorityScore(
        int dueCards,
        int overdueCards,
        int newCards,
        int lapsedCards,
        decimal? accuracy,
        DateTime? lastReviewedAtUtc,
        DateTime now)
    {
        var incorrectPressure = accuracy.HasValue ? (100m - accuracy.Value) / 10m : 0m;
        var recentlyStudiedPenalty = lastReviewedAtUtc >= now.AddDays(-1) ? 2m : 0m;
        var score = dueCards * 3m
            + overdueCards * 5m
            + newCards
            + lapsedCards * 4m
            + incorrectPressure
            - recentlyStudiedPenalty;

        return decimal.Round(Math.Max(0m, score), 2);
    }

    private static decimal CalculateAccuracy(CardReviewState? state)
    {
        if (state is null || state.TotalReviews == 0)
        {
            return 0m;
        }

        return decimal.Round(state.CorrectReviews * 100m / state.TotalReviews, 2);
    }

    private sealed record StudyPlanCard(
        Guid Id,
        Guid DeckId,
        string Type,
        string FrontText,
        string BackText,
        string? ExampleSentence,
        List<CardChoiceDto> Choices,
        int Order);

    private static class StudyPlanReasons
    {
        public const string Due = "Due";
        public const string Lapsed = "Lapsed";
        public const string New = "New";
        public const string Review = "Review";
    }
}
