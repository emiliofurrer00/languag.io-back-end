using Languag.io.Api.Contracts.Decks;

namespace Languag.io.Application.StudySessions;

public sealed record StudyPlanCardDto(
    Guid CardId,
    Guid DeckId,
    string Type,
    string FrontText,
    string BackText,
    string? ExampleSentence,
    List<CardChoiceDto> Choices,
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
