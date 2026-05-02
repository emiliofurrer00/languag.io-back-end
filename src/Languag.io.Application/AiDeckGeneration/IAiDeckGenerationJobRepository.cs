using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiDeckGeneration;

public interface IAiDeckGenerationJobRepository
{
    Task AddAsync(AiDeckGenerationJob job, CancellationToken ct = default);
    Task<AiDeckGenerationJob?> GetForUserAsync(Guid jobId, Guid userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
