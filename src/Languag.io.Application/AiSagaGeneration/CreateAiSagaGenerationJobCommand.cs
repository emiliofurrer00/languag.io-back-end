namespace Languag.io.Application.AiSagaGeneration;

public record CreateAiSagaGenerationJobCommand(
    string Prompt,
    string? TargetLanguage,
    string? NativeLanguage,
    string? Difficulty,
    int DeckCount,
    int CardsPerDeck,
    bool IncludeAudio,
    int MultiChoiceCountPerDeck = 0);
