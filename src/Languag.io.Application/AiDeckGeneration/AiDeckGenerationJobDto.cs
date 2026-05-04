namespace Languag.io.Application.AiDeckGeneration;

public record AiDeckGenerationJobDto(
    Guid Id,
    string Status,
    Guid? CreatedDeckId,
    string? ErrorMessage,
    string AudioStatus,
    int RequestedCardCount,
    int RequestedMultiChoiceCount,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
