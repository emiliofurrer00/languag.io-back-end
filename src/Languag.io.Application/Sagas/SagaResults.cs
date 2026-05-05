namespace Languag.io.Application.Sagas;

public sealed record CreateSagaResult(
    CreateSagaStatus Status,
    Guid? SagaId = null,
    string? Error = null);

public enum CreateSagaStatus
{
    Created,
    Invalid,
    DeckNotFound
}

public sealed record CompleteSagaLessonResult(
    CompleteSagaLessonStatus Status,
    SagaProgressDto? Progress = null,
    string? Error = null);

public enum CompleteSagaLessonStatus
{
    Completed,
    SagaNotFound,
    LessonNotFound,
    Invalid
}
