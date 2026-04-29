namespace Languag.io.Application.StudySessions;

public sealed record DeckStudyRecommendationDto(
    Guid DeckId,
    string Title,
    string Category,
    string? Description,
    string? Color,
    int TotalCards,
    int DueCards,
    int NewCards,
    int LapsedCards,
    int OverdueCards,
    DateTime? NextDueAtUtc,
    decimal? Accuracy,
    decimal PriorityScore);
