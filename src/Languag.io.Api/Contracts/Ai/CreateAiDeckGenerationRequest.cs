namespace Languag.io.Api.Contracts.Ai;

public record CreateAiDeckGenerationRequest(
    string Prompt,
    string? TargetLanguage,
    string? NativeLanguage,
    string? Difficulty,
    int CardCount);
