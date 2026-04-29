namespace Languag.io.Api.Contracts.Decks;

public record CardDto(
    Guid Id,
    string Type,
    string FrontText,
    string BackText,
    string? ExampleSentence,
    List<CardChoiceDto> Choices,
    int Order
);

public record CardChoiceDto(
    Guid Id,
    string Text,
    bool IsCorrect,
    int Order
);
