namespace Languag.io.Application.StudySessions;

public interface IStudySessionService
{
    Task<SubmitStudySessionResult> SubmitAsync(
        SubmitStudySessionCommand command,
        Guid userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<StudyPlanCardDto>?> GetDeckStudyPlanAsync(
        Guid deckId,
        Guid userId,
        int limit,
        CancellationToken ct = default);

    Task<IReadOnlyList<DeckStudyRecommendationDto>> GetStudyRecommendationsAsync(
        Guid userId,
        int limit,
        CancellationToken ct = default);
}
