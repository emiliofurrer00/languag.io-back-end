namespace Languag.io.Application.AiSagaGeneration;

public record AiSagaGenerationJobDto(
    Guid Id,
    string Status,
    Guid? CreatedSagaId,
    string? ErrorMessage,
    string AudioStatus,
    int RequestedDeckCount,
    int RequestedCardsPerDeck,
    int RequestedMultiChoiceCountPerDeck,
    DateTime UsageWeekStartUtc,
    DateTime NextAllowedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);
