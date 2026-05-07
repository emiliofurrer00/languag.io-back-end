using Languag.io.Application.AiSagaGeneration;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Languag.io.Infrastructure.Repositories;

public class AiSagaGenerationJobRepository : IAiSagaGenerationJobRepository
{
    private readonly AppDbContext _dbContext;

    public AiSagaGenerationJobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AiSagaGenerationJob job, CancellationToken ct = default)
    {
        await _dbContext.AiSagaGenerationJobs.AddAsync(job, ct);
    }

    public Task<AiSagaGenerationJob?> GetForUserAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default)
    {
        return _dbContext.AiSagaGenerationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId && job.UserId == userId, ct);
    }

    public Task<AiSagaGenerationJob?> GetForUserWeekAsync(
        Guid userId,
        DateTime usageWeekStartUtc,
        CancellationToken ct = default)
    {
        return _dbContext.AiSagaGenerationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                job => job.UserId == userId && job.UsageWeekStartUtc == usageWeekStartUtc,
                ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsWeeklyLimitViolation(ex))
        {
            throw new AiSagaGenerationWeeklyLimitExceededException(ex);
        }
    }

    private static bool IsWeeklyLimitViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
            string.Equals(
                postgresException.ConstraintName,
                "IX_AiSagaGenerationJobs_UserId_UsageWeekStartUtc",
                StringComparison.Ordinal);
    }
}
