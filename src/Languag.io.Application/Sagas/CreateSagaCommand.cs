using Languag.io.Domain.Entities;

namespace Languag.io.Application.Sagas;

public sealed record CreateSagaCommand(
    string Title,
    string? Description,
    string Category,
    string? Color,
    DeckVisibility Visibility,
    IReadOnlyList<CreateSagaChapterCommand> Chapters);

public sealed record CreateSagaChapterCommand(
    string Title,
    string? Description,
    int Order,
    IReadOnlyList<CreateSagaLessonCommand> Lessons);

public sealed record CreateSagaLessonCommand(
    Guid DeckId,
    string? Title,
    string? Description,
    int Order);
