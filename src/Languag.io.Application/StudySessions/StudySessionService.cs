using Languag.io.Application.ActivityLogs;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public sealed class StudySessionService : IStudySessionService
{
    private readonly IStudySessionRepository _studySessionRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public StudySessionService(
        IStudySessionRepository studySessionRepository,
        IActivityLogRepository activityLogRepository)
    {
        _studySessionRepository = studySessionRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<SubmitStudySessionResult> SubmitAsync(
        SubmitStudySessionCommand command,
        Guid userId,
        CancellationToken ct = default)
    {
        if (command.PercentageCorrect < 0m || command.PercentageCorrect > 100m)
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "PercentageCorrect must be between 0 and 100.");
        }

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

        var now = DateTime.UtcNow;
        var isFirstStudySession = !await _studySessionRepository.UserHasStudySessionsAsync(userId, ct);
        var studySession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = command.DeckId,
            DeckVersionId = deckVersion.DeckVersionId,
            UserId = userId,
            CreatedAtUtc = now,
            PercentageCorrect = decimal.Round(command.PercentageCorrect, 2),
            Responses = command.Responses.Select(response =>
                {
                    var deckVersionCard = deckVersionCardsBySubmittedId[response.CardId];
                    return new StudySessionResponse
                    {
                        Id = Guid.NewGuid(),
                        StudySessionId = Guid.Empty,
                        DeckId = command.DeckId,
                        CardId = deckVersionCard.ReviewCardId,
                        DeckVersionCardId = deckVersionCard.DeckVersionCardId,
                        UserId = userId,
                        WasCorrect = response.WasCorrect
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

        await UpdateReviewStatesAsync(command, userId, now, deckVersionCardsBySubmittedId, ct);
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
            DateTime.UtcNow,
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
            DateTime.UtcNow,
            NormalizeLimit(limit),
            ct);
    }

    private async Task UpdateReviewStatesAsync(
        SubmitStudySessionCommand command,
        Guid userId,
        DateTime now,
        IReadOnlyDictionary<Guid, DeckVersionCardStudyReference> deckVersionCardsBySubmittedId,
        CancellationToken ct)
    {
        var reviewableResponses = command.Responses
            .Select(response => new
            {
                Response = response,
                DeckVersionCard = deckVersionCardsBySubmittedId[response.CardId]
            })
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
            command.DeckId,
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
                    DeckId = command.DeckId,
                    CardId = cardId,
                    DueAtUtc = now,
                    EaseFactor = 2.5m
                };
                statesByCardId.Add(cardId, state);
                newStates.Add(state);
            }

            ApplyReviewResult(state, item.Response.WasCorrect, now);
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

    private static void ApplyReviewResult(CardReviewState state, bool wasCorrect, DateTime now)
    {
        if (!wasCorrect)
        {
            state.RepetitionCount = 0;
            state.LapseCount++;
            state.IntervalDays = 1;
            state.EaseFactor = Math.Max(1.3m, state.EaseFactor - 0.2m);
        }
        else
        {
            state.RepetitionCount++;
            state.IntervalDays = state.RepetitionCount switch
            {
                1 => 1,
                2 => 3,
                _ => Math.Max(1, (int)Math.Round(state.IntervalDays * state.EaseFactor))
            };
            state.EaseFactor = Math.Min(3.0m, state.EaseFactor + 0.05m);
        }

        state.LastReviewedAtUtc = now;
        state.DueAtUtc = now.AddDays(state.IntervalDays);
        state.TotalReviews++;
        if (wasCorrect)
        {
            state.CorrectReviews++;
        }
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
}
