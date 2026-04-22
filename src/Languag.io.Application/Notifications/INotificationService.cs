using Languag.io.Application.Common;

namespace Languag.io.Application.Notifications;

public interface INotificationService
{
    Task<CursorPage<NotificationDto>> GetNotificationsAsync(
        GetNotificationsQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<UnreadNotificationCountDto> GetUnreadNotificationCountAsync(
        GetUnreadNotificationCountQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<MarkNotificationReadResult> MarkNotificationReadAsync(
        MarkNotificationReadCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<int> MarkAllNotificationsReadAsync(
        MarkAllNotificationsReadCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
}
