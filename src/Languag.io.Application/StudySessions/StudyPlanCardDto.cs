namespace Languag.io.Application.StudySessions;

public sealed record StudyPlanCardDto(
    Guid CardId,
    Guid DeckId,
    string FrontText,
    string BackText,
    string? ExampleSentence,
    int Order,
    bool IsNew,
    bool IsDue,
    DateTime? LastReviewedAtUtc,
    DateTime? DueAtUtc,
    int IntervalDays,
    decimal EaseFactor,
    int RepetitionCount,
    int LapseCount,
    int TotalReviews,
    int CorrectReviews,
    decimal Accuracy,
    string Reason);