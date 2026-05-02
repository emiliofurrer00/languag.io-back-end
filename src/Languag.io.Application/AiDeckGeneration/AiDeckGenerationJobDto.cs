namespace Languag.io.Application.AiDeckGeneration;

public record AiDeckGenerationJobDto(
    Guid Id,
    string Status,
    Guid? CreatedDeckId,
    string? ErrorMessage,
    int RequestedCardCount,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
