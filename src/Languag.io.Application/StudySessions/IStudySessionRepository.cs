using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public interface IStudySessionRepository
{
    Task<bool> CanAccessDeckAsync(Guid deckId, Guid userId, CancellationToken ct = default);
    Task<bool> DeckContainsCardsAsync(Guid deckId, IReadOnlyCollection<Guid> cardIds, CancellationToken ct = default);
    Task<bool> UserHasStudySessionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CardReviewState>> GetReviewStatesAsync(
        Guid userId,
        Guid deckId,
        IReadOnlyCollection<Guid> cardIds,
        CancellationToken ct = default);

    Task AddReviewStatesAsync(IReadOnlyCollection<CardReviewState> reviewStates, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCardDto>?> GetDeckStudyPlanAsync(
        Guid deckId,
        Guid userId,
        DateTime now,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeckStudyRecommendationDto>> GetStudyRecommendationsAsync(
        Guid userId,
        DateTime now,
        int limit,
        CancellationToken ct = default);

    Task AddAsync(StudySession studySession, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
