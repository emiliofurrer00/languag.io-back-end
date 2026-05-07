namespace Languag.io.Application.AiSagaGeneration;

public sealed record CreateAiSagaGenerationResult(
    CreateAiSagaGenerationStatus Status,
    Guid? JobId = null,
    string? Error = null,
    DateTime? NextAllowedAtUtc = null);

public enum CreateAiSagaGenerationStatus
{
    Created,
    WeeklyLimitExceeded,
    Invalid
}
