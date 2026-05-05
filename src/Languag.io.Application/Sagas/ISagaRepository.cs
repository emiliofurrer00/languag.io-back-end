using Languag.io.Domain.Entities;

namespace Languag.io.Application.Sagas;

public interface ISagaRepository
{
    Task<IReadOnlyList<SagaDto>> GetPublicSagasAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SagaDto>> GetVisibleSagasAsync(Guid userId, CancellationToken ct = default);
    Task<SagaDto?> GetSagaByIdAsync(Guid sagaId, Guid? currentUserId, CancellationToken ct = default);
    Task<Saga?> GetVisibleSagaForProgressAsync(Guid sagaId, Guid userId, CancellationToken ct = default);
    Task<SagaProgress?> GetProgressForUpdateAsync(Guid sagaId, Guid userId, CancellationToken ct = default);
    Task<bool> CanAccessDecksAsync(IReadOnlyCollection<Guid> deckIds, Guid userId, CancellationToken ct = default);
    Task<bool> AreDecksPublicAsync(IReadOnlyCollection<Guid> deckIds, CancellationToken ct = default);
    Task AddAsync(Saga saga, CancellationToken ct = default);
    Task AddProgressAsync(SagaProgress progress, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
