namespace Languag.io.Api.Contracts.Ai;

public record CreateAiSagaGenerationRequest(
    string Prompt,
    string? TargetLanguage,
    string? NativeLanguage,
    string? Difficulty,
    int DeckCount = 3,
    int CardsPerDeck = 10,
    bool IncludeAudio = false,
    int MultiChoiceCountPerDeck = 0);
