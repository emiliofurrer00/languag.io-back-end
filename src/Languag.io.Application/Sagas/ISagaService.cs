namespace Languag.io.Application.Sagas;

public interface ISagaService
{
    Task<IReadOnlyList<SagaDto>> GetPublicSagasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SagaDto>> GetVisibleSagasAsync(Guid userId, CancellationToken ct = default);
    Task<SagaDto?> GetSagaByIdAsync(Guid sagaId, Guid? currentUserId, CancellationToken ct = default);
    Task<CreateSagaResult> CreateSagaAsync(CreateSagaCommand command, Guid ownerId, CancellationToken ct = default);
    Task<CompleteSagaLessonResult> CompleteLessonAsync(Guid sagaId, Guid lessonId, Guid userId, CancellationToken ct = default);
}
