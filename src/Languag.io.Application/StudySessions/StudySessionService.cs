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

        var deckContainsCards = await _studySessionRepository.DeckContainsCardsAsync(
            command.DeckId,
            cardIds,
            ct);

        if (!deckContainsCards)
        {
            return new SubmitStudySessionResult(
                SubmitStudySessionStatus.Invalid,
                Error: "One or more responses reference cards that do not belong to the deck.");
        }

        var now = DateTime.UtcNow;
        var isFirstStudySession = !await _studySessionRepository.UserHasStudySessionsAsync(userId, ct);
        var studySession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = command.DeckId,
            UserId = userId,
            CreatedAtUtc = now,
            PercentageCorrect = decimal.Round(command.PercentageCorrect, 2),
            Responses = command.Responses.Select(response => new StudySessionResponse
            {
                Id = Guid.NewGuid(),
                StudySessionId = Guid.Empty,
                DeckId = command.DeckId,
                CardId = response.CardId,
                UserId = userId,
                WasCorrect = response.WasCorrect
            }).ToList()
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

        await _studySessionRepository.SaveChangesAsync(ct);

        return new SubmitStudySessionResult(
            SubmitStudySessionStatus.Created,
            StudySessionId: studySession.Id);
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
