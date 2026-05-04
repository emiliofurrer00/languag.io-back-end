namespace Languag.io.Application.AiDeckGeneration;

public record CreateAiDeckGenerationJobCommand(
    string Prompt,
    string? TargetLanguage,
    string? NativeLanguage,
    string? Difficulty,
    int CardCount,
    bool IncludeAudio,
    int MultiChoiceCount = 0);
