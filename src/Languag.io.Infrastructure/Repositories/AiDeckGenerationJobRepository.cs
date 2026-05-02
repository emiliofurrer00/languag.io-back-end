using Languag.io.Application.AiDeckGeneration;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public class AiDeckGenerationJobRepository : IAiDeckGenerationJobRepository
{
    private readonly AppDbContext _dbContext;

    public AiDeckGenerationJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AiDeckGenerationJob job, CancellationToken ct = default)
    {
        await _dbContext.AiDeckGenerationJobs.AddAsync(job, ct);
    }

    public Task<AiDeckGenerationJob?> GetForUserAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default)
    {
        return _dbContext.AiDeckGenerationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId && job.UserId == userId, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}
