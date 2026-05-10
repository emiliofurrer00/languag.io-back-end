using Languag.io.Application.ActivityLogs;
using Languag.io.Application.Common;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public sealed class StudySessionService : IStudySessionService
{
    private readonly IStudySessionRepository _studySessionRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IClock _clock;
    private readonly ICardReviewScheduler _cardReviewScheduler;

    public StudySessionService(
        IStudySessionRepository studySessionRepository,
        IActivityLogRepository activityLogRepository,
        IClock clock,
        ICardReviewScheduler cardReviewScheduler)
    {
        _studySessionRepository = studySessionRepository;
        _activityLogRepository = activityLogRepository;
        _clock = clock;
        _cardReviewScheduler = cardReviewScheduler;
    }

    public async Task<SubmitStudySessionResult> SubmitAsync(
        SubmitStudySessionCommand command,
        Guid userId,
        CancellationToken ct = default)
    {
        if (command.Responses.Count == 0)
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "A study session must include at least one response.");
        }

        var canAccessDeck = await _studySessionRepository.CanAccessDeckAsync(command.DeckId, userId, ct);
        if (!canAccessDeck)
        {
            return new SubmitStudySessionResult(SubmitStudySessionStatus.DeckNotFound);
        }

        var cardIds = command.Responses
            .Select(response => response.CardId)
            .Distinct()
            .ToArray();

        var deckVersion = await _studySessionRepository.GetDeckVersionForStudyAsync(
            command.DeckId,
            command.DeckVersionId,
            ct);

        if (deckVersion is null)
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "Deck version was not found.");
        }

        var deckVersionCards = await _studySessionRepository.GetDeckVersionCardReferencesAsync(
            deckVersion.DeckVersionId,
            cardIds,
            ct);
        var deckVersionCardsBySubmittedId = BuildSubmittedCardLookup(deckVersionCards);

        if (cardIds.Any(cardId => !deckVersionCardsBySubmittedId.ContainsKey(cardId)))
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "One or more responses reference cards that do not belong to the deck version.");
        }

        var resolvedResponses = command.Responses
            .Select(response => new ResolvedStudyResponse(
                response,
                deckVersionCardsBySubmittedId[response.CardId]))
            .ToArray();

        if (resolvedResponses
            .GroupBy(item => item.DeckVersionCard.DeckVersionCardId)
            .Any(group => group.Count() > 1))
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "A study session cannot include duplicate responses for the same card.");
        }

        var now = _clock.UtcNow;
        var isFirstStudySession = !await _studySessionRepository.UserHasStudySessionsAsync(userId, ct);
        var studySession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = command.DeckId,
            DeckVersionId = deckVersion.DeckVersionId,
            UserId = userId,
            CreatedAtUtc = now,
            PercentageCorrect = CalculatePercentageCorrect(command.Responses),
            Responses = resolvedResponses.Select(item =>
                {
                    return new StudySessionResponse
                    {
                        Id = Guid.NewGuid(),
                        StudySessionId = Guid.Empty,
                        DeckId = command.DeckId,
                        CardId = item.DeckVersionCard.ReviewCardId,
                        DeckVersionCardId = item.DeckVersionCard.DeckVersionCardId,
                        UserId = userId,
                        WasCorrect = item.Response.WasCorrect
                    };
                })
                .ToList()
        };

        foreach (var response in studySession.Responses)
        {
            response.StudySessionId = studySession.Id;
        }

        await _studySessionRepository.AddAsync(studySession, ct);
        await _activityLogRepository.AddAsync(
            CreateActivityLog(userId, command.DeckId, ActivityType.DeckStudySessionCompleted, now),
            ct);

        if (studySession.PercentageCorrect == 100m)
        {
            await _activityLogRepository.AddAsync(
                CreateActivityLog(userId, command.DeckId, ActivityType.DeckMastered, now),
                ct);
        }

        if (isFirstStudySession)
        {
            await _activityLogRepository.AddAsync(
                CreateActivityLog(userId, command.DeckId, ActivityType.FirstStudySessionCompleted, now),
                ct);
        }

        await UpdateReviewStatesAsync(command.DeckId, userId, now, resolvedResponses, ct);
        await _studySessionRepository.SaveChangesAsync(ct);

        return new SubmitStudySessionResult(
            SubmitStudySessionStatus.Created,
            StudySessionId: studySession.Id);
    }

    public async Task<IReadOnlyList<StudyPlanCardDto>?> GetDeckStudyPlanAsync(
        Guid deckId,
        Guid userId,
        int limit,
        CancellationToken ct = default)
    {
        if (!await _studySessionRepository.CanAccessDeckAsync(deckId, userId, ct))
        {
            return null;
        }

        return await _studySessionRepository.GetDeckStudyPlanAsync(
            deckId,
            userId,
            _clock.UtcNow,
            NormalizeLimit(limit),
            ct);
    }

    public Task<IReadOnlyList<DeckStudyRecommendationDto>> GetStudyRecommendationsAsync(
        Guid userId,
        int limit,
        CancellationToken ct = default)
    {
        return _studySessionRepository.GetStudyRecommendationsAsync(
            userId,
            _clock.UtcNow,
            NormalizeLimit(limit),
            ct);
    }

    private async Task UpdateReviewStatesAsync(
        Guid deckId,
        Guid userId,
        DateTime now,
        IReadOnlyCollection<ResolvedStudyResponse> resolvedResponses,
        CancellationToken ct)
    {
        var reviewableResponses = resolvedResponses
            .Where(item => item.DeckVersionCard.ReviewCardId.HasValue)
            .ToArray();

        if (reviewableResponses.Length == 0)
        {
            return;
        }

        var cardIds = reviewableResponses
            .Select(item => item.DeckVersionCard.ReviewCardId!.Value)
            .Distinct()
            .ToArray();

        var existingStates = await _studySessionRepository.GetReviewStatesAsync(
            userId,
            deckId,
            cardIds,
            ct);

        var statesByCardId = existingStates.ToDictionary(state => state.CardId);
        var newStates = new List<CardReviewState>();

        foreach (var item in reviewableResponses)
        {
            var cardId = item.DeckVersionCard.ReviewCardId!.Value;
            if (!statesByCardId.TryGetValue(cardId, out var state))
            {
                state = new CardReviewState
                {
                    UserId = userId,
                    DeckId = deckId,
                    CardId = cardId,
                    DueAtUtc = now,
                    EaseFactor = 2.5m
                };
                statesByCardId.Add(cardId, state);
                newStates.Add(state);
            }

            _cardReviewScheduler.ApplyReviewResult(state, item.Response.WasCorrect, now);
        }

        if (newStates.Count > 0)
        {
            await _studySessionRepository.AddReviewStatesAsync(newStates, ct);
        }
    }

    private static Dictionary<Guid, DeckVersionCardStudyReference> BuildSubmittedCardLookup(
        IReadOnlyCollection<DeckVersionCardStudyReference> deckVersionCards)
    {
        var lookup = new Dictionary<Guid, DeckVersionCardStudyReference>();
        foreach (var deckVersionCard in deckVersionCards)
        {
            lookup.TryAdd(deckVersionCard.DeckVersionCardId, deckVersionCard);
            if (deckVersionCard.OriginalCardId.HasValue)
            {
                lookup.TryAdd(deckVersionCard.OriginalCardId.Value, deckVersionCard);
            }
        }

        return lookup;
    }

    private static decimal CalculatePercentageCorrect(IReadOnlyCollection<SubmitStudySessionResponseCommand> responses)
    {
        var correctCount = responses.Count(response => response.WasCorrect);
        return decimal.Round(correctCount * 100m / responses.Count, 2);
    }

    private static int NormalizeLimit(int limit)
    {
        return Math.Clamp(limit, 1, 100);
    }

    private static ActivityLog CreateActivityLog(
        Guid userId,
        Guid deckId,
        ActivityType activityType,
        DateTime occurredAtUtc)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeckId = deckId,
            Type = activityType,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = occurredAtUtc
        };
    }

    private sealed record ResolvedStudyResponse(
        SubmitStudySessionResponseCommand Response,
        DeckVersionCardStudyReference DeckVersionCard);
}
