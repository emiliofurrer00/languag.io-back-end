using Languag.io.Application.ActivityLogs;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;

namespace Languag.io.Infrastructure.Repositories;

public sealed class ActivityLogRepository : IActivityLogRepository
{
    private readonly AppDbContext _dbContext;

    public ActivityLogRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ActivityLog activityLog, CancellationToken ct = default)
    {
        await _dbContext.ActivityLogs.AddAsync(activityLog, ct);
    }
}
