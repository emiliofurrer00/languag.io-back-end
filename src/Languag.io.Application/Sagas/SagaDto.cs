using Languag.io.Domain.Entities;

namespace Languag.io.Application.Sagas;

public sealed record SagaDto(
    Guid Id,
    string Title,
    string Category,
    string? Description,
    DeckVisibility Visibility,
    string? Color,
    IReadOnlyList<SagaChapterDto> Chapters,
    string OwnerUsername,
    string OwnerName,
    bool IsOwner,
    SagaProgressDto Progress);

public sealed record SagaChapterDto(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    IReadOnlyList<SagaLessonDto> Lessons);

public sealed record SagaLessonDto(
    Guid Id,
    Guid DeckId,
    string DeckTitle,
    string? Title,
    string? Description,
    int Order,
    int CardCount);

public sealed record SagaProgressDto(
    Guid? LastStudiedLessonId,
    Guid? HighestCompletedLessonId,
    Guid? CurrentLessonId,
    int CompletedLessonCount,
    int TotalLessonCount,
    decimal PercentageComplete,
    DateTime? StartedAtUtc,
    DateTime? LastStudiedAtUtc,
    DateTime? CompletedAtUtc);
