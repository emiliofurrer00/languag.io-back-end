using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiSagaGeneration;

public interface IAiSagaGenerationJobRepository
{
    Task AddAsync(AiSagaGenerationJob job, CancellationToken ct = default);
    Task<AiSagaGenerationJob?> GetForUserAsync(Guid jobId, Guid userId, CancellationToken ct = default);
    Task<AiSagaGenerationJob?> GetForUserWeekAsync(Guid userId, DateTime usageWeekStartUtc, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
