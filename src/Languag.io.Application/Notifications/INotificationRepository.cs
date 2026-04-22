using Languag.io.Application.Common;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default);
    Task<CursorPage<NotificationDto>> GetNotificationsAsync(
        Guid currentUserId,
        GetNotificationsQuery query,
        CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid currentUserId, DateTime readAtUtc, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
