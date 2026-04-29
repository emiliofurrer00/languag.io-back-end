using Languag.io.Application.ActivityLogs;
using Languag.io.Application.StudySessions;
using Languag.io.Domain.Entities;

namespace Languag.io.Tests;

public sealed class StudySessionServiceTests
{
    [Fact]
    public async Task SubmitAsync_CreatesStudySessionAndAllExpectedActivityLogs()
    {
        var repository = new CapturingStudySessionRepository();
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new StudySessionService(repository, activityLogRepository);
        var userId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var result = await service.SubmitAsync(
            new SubmitStudySessionCommand(
                deckId,
                100m,
                [
                    new SubmitStudySessionResponseCommand(cardId, true)
                ]),
            userId);

        Assert.Equal(SubmitStudySessionStatus.Created, result.Status);
        Assert.NotNull(result.StudySessionId);
        Assert.True(repository.SaveChangesCalled);
        Assert.NotNull(repository.AddedStudySession);
        Assert.Equal(deckId, repository.AddedStudySession!.DeckId);
        Assert.Equal(userId, repository.AddedStudySession.UserId);
        Assert.Equal(100m, repository.AddedStudySession.PercentageCorrect);

        var response = Assert.Single(repository.AddedStudySession.Responses);
        Assert.Equal(result.StudySessionId, response.StudySessionId);
        Assert.Equal(deckId, response.DeckId);
        Assert.Equal(cardId, response.CardId);
        Assert.Equal(userId, response.UserId);
        Assert.True(response.WasCorrect);

        Assert.Collection(
            activityLogRepository.AddedLogs,
            activity => Assert.Equal(ActivityType.DeckStudySessionCompleted, activity.Type),
            activity => Assert.Equal(ActivityType.DeckMastered, activity.Type),
            activity => Assert.Equal(ActivityType.FirstStudySessionCompleted, activity.Type));

        var reviewState = Assert.Single(repository.ReviewStates);
        Assert.Equal(userId, reviewState.UserId);
        Assert.Equal(deckId, reviewState.DeckId);
        Assert.Equal(cardId, reviewState.CardId);
        Assert.Equal(1, reviewState.IntervalDays);
        Assert.Equal(2.55m, reviewState.EaseFactor);
        Assert.Equal(1, reviewState.RepetitionCount);
        Assert.Equal(0, reviewState.LapseCount);
        Assert.Equal(1, reviewState.TotalReviews);
        Assert.Equal(1, reviewState.CorrectReviews);
        Assert.NotNull(reviewState.LastReviewedAtUtc);
        Assert.True(reviewState.DueAtUtc > reviewState.LastReviewedAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_SkipsConditionalActivityLogsWhenNotApplicable()
    {
        var repository = new CapturingStudySessionRepository
        {
            UserHasStudySessionsResult = true
        };
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new StudySessionService(repository, activityLogRepository);

        var result = await service.SubmitAsync(
            new SubmitStudySessionCommand(
                Guid.NewGuid(),
                80m,
                [
                    new SubmitStudySessionResponseCommand(Guid.NewGuid(), true)
                ]),
            Guid.NewGuid());

        Assert.Equal(SubmitStudySessionStatus.Created, result.Status);
        var activity = Assert.Single(activityLogRepository.AddedLogs);
        Assert.Equal(ActivityType.DeckStudySessionCompleted, activity.Type);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsInvalidWhenResponseCardsDoNotBelongToDeck()
    {
        var repository = new CapturingStudySessionRepository
        {
            DeckContainsCardsResult = false
        };
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new StudySessionService(repository, activityLogRepository);

        var result = await service.SubmitAsync(
            new SubmitStudySessionCommand(
                Guid.NewGuid(),
                50m,
                [
                    new SubmitStudySessionResponseCommand(Guid.NewGuid(), false)
                ]),
            Guid.NewGuid());

        Assert.Equal(SubmitStudySessionStatus.Invalid, result.Status);
        Assert.Null(repository.AddedStudySession);
        Assert.Empty(activityLogRepository.AddedLogs);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsDeckNotFoundWhenDeckIsNotAccessible()
    {
        var repository = new CapturingStudySessionRepository
        {
            CanAccessDeckResult = false
        };
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new StudySessionService(repository, activityLogRepository);

        var result = await service.SubmitAsync(
            new SubmitStudySessionCommand(
                Guid.NewGuid(),
                50m,
                [
                    new SubmitStudySessionResponseCommand(Guid.NewGuid(), false)
                ]),
            Guid.NewGuid());

        Assert.Equal(SubmitStudySessionStatus.DeckNotFound, result.Status);
        Assert.Null(repository.AddedStudySession);
        Assert.Empty(activityLogRepository.AddedLogs);
    }

    [Fact]
    public async Task SubmitAsync_UpdatesExistingReviewStateWhenCardIsIncorrect()
    {
        var userId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var existingState = new CardReviewState
        {
            UserId = userId,
            DeckId = deckId,
            CardId = cardId,
            DueAtUtc = DateTime.UtcNow.AddDays(4),
            IntervalDays = 6,
            EaseFactor = 2.5m,
            RepetitionCount = 3,
            TotalReviews = 3,
            CorrectReviews = 3
        };
        var repository = new CapturingStudySessionRepository
        {
            ReviewStates = [existingState]
        };
        var service = new StudySessionService(repository, new CapturingActivityLogRepository());

        var result = await service.SubmitAsync(
            new SubmitStudySessionCommand(
                deckId,
                0m,
                [
                    new SubmitStudySessionResponseCommand(cardId, false)
                ]),
            userId);

        Assert.Equal(SubmitStudySessionStatus.Created, result.Status);
        Assert.Same(existingState, Assert.Single(repository.ReviewStates));
        Assert.Empty(repository.AddedReviewStates);
        Assert.Equal(1, existingState.IntervalDays);
        Assert.Equal(2.3m, existingState.EaseFactor);
        Assert.Equal(0, existingState.RepetitionCount);
        Assert.Equal(1, existingState.LapseCount);
        Assert.Equal(4, existingState.TotalReviews);
        Assert.Equal(3, existingState.CorrectReviews);
    }

    private sealed class CapturingStudySessionRepository : IStudySessionRepository
    {
        public bool CanAccessDeckResult { get; init; } = true;
        public bool DeckContainsCardsResult { get; init; } = true;
        public bool UserHasStudySessionsResult { get; init; }
        public bool SaveChangesCalled { get; private set; }
        public StudySession? AddedStudySession { get; private set; }
        public List<CardReviewState> ReviewStates { get; init; } = [];
        public List<CardReviewState> AddedReviewStates { get; } = [];

        public Task<bool> CanAccessDeckAsync(Guid deckId, Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(CanAccessDeckResult);
        }

        public Task<bool> DeckContainsCardsAsync(
            Guid deckId,
            IReadOnlyCollection<Guid> cardIds,
            CancellationToken ct = default)
        {
            return Task.FromResult(DeckContainsCardsResult);
        }

        public Task<bool> UserHasStudySessionsAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(UserHasStudySessionsResult);
        }

        public Task<IReadOnlyList<CardReviewState>> GetReviewStatesAsync(
            Guid userId,
            Guid deckId,
            IReadOnlyCollection<Guid> cardIds,
            CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<CardReviewState>>(
                ReviewStates
                    .Where(state => state.UserId == userId &&
                        state.DeckId == deckId &&
                        cardIds.Contains(state.CardId))
                    .ToList());
        }

        public Task AddReviewStatesAsync(
            IReadOnlyCollection<CardReviewState> reviewStates,
            CancellationToken ct = default)
        {
            AddedReviewStates.AddRange(reviewStates);
            ReviewStates.AddRange(reviewStates);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<StudyPlanCardDto>?> GetDeckStudyPlanAsync(
            Guid deckId,
            Guid userId,
            DateTime now,
            int limit,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<DeckStudyRecommendationDto>> GetStudyRecommendationsAsync(
            Guid userId,
            DateTime now,
            int limit,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(StudySession studySession, CancellationToken ct = default)
        {
            AddedStudySession = studySession;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingActivityLogRepository : IActivityLogRepository
    {
        public List<ActivityLog> AddedLogs { get; } = [];

        public Task AddAsync(ActivityLog activityLog, CancellationToken ct = default)
        {
            AddedLogs.Add(activityLog);
            return Task.CompletedTask;
        }
    }
}
