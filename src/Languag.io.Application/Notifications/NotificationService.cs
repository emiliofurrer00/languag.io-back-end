using Languag.io.Application.Common;

namespace Languag.io.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task<CursorPage<NotificationDto>> GetNotificationsAsync(
        GetNotificationsQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        return _notificationRepository.GetNotificationsAsync(currentUserId, query, ct);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadNotificationCountAsync(
        GetUnreadNotificationCountQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var count = await _notificationRepository.GetUnreadCountAsync(currentUserId, ct);
        return new UnreadNotificationCountDto(count);
    }

    public async Task<MarkNotificationReadResult> MarkNotificationReadAsync(
        MarkNotificationReadCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(command.NotificationId, ct);
        if (notification is null)
        {
            return new MarkNotificationReadResult(MarkNotificationReadStatus.NotFound);
        }

        if (notification.UserId != currentUserId)
        {
            return new MarkNotificationReadResult(MarkNotificationReadStatus.Forbidden);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _notificationRepository.SaveChangesAsync(ct);
        }

        return new MarkNotificationReadResult(MarkNotificationReadStatus.Success);
    }

    public Task<int> MarkAllNotificationsReadAsync(
        MarkAllNotificationsReadCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        return _notificationRepository.MarkAllAsReadAsync(currentUserId, DateTime.UtcNow, ct);
    }
}
