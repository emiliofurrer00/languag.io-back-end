using Languag.io.Domain.Entities;

namespace Languag.io.Application.ActivityLogs;

public interface IActivityLogRepository
{
    Task AddAsync(ActivityLog activityLog, CancellationToken ct = default);
}
