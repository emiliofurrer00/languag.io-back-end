using Languag.io.Application.Common;
using Languag.io.Application.Notifications;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private const int MaximumPageSize = 100;
    private readonly AppDbContext _dbContext;

    public NotificationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _dbContext.Notifications.AddAsync(notification, ct);
    }

    public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default)
    {
        return _dbContext.Notifications
            .SingleOrDefaultAsync(notification => notification.Id == notificationId, ct);
    }

    public async Task<CursorPage<NotificationDto>> GetNotificationsAsync(
        Guid currentUserId,
        GetNotificationsQuery query,
        CancellationToken ct = default)
    {
        var entityQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == currentUserId);

        if (query.Cursor is { } pageCursor)
        {
            entityQuery = entityQuery.Where(notification =>
                notification.CreatedAtUtc < pageCursor.CreatedAtUtc
                || (notification.CreatedAtUtc == pageCursor.CreatedAtUtc
                    && notification.Id.CompareTo(pageCursor.Id) < 0));
        }

        var pageSize = Math.Clamp(query.PageSize, 1, MaximumPageSize);
        var items = await entityQuery
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => new
            {
                notification.Id,
                notification.CreatedAtUtc,
                notification.Type,
                notification.ActorUserId,
                ActorUsername = notification.ActorUser != null ? notification.ActorUser.Username : null,
                ActorName = notification.ActorUser != null ? notification.ActorUser.Name : null,
                ActorEmail = notification.ActorUser != null ? notification.ActorUser.Email : null,
                ActorExternalId = notification.ActorUser != null ? notification.ActorUser.ExternalId : null,
                notification.EntityType,
                notification.EntityId,
                notification.Title,
                notification.Body,
                notification.IsRead
            })
            .Take(pageSize + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var notifications = items
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Type,
                notification.ActorUserId,
                BuildDisplayName(notification.ActorUsername, notification.ActorName, notification.ActorEmail, notification.ActorExternalId),
                null,
                notification.EntityType,
                notification.EntityId,
                notification.Title,
                notification.Body,
                notification.IsRead,
                notification.CreatedAtUtc))
            .ToArray();

        var nextCursor = hasMore
            ? new TimelineCursor(items[^1].CreatedAtUtc, items[^1].Id).Encode()
            : null;

        return new CursorPage<NotificationDto>(notifications, nextCursor);
    }

    public Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken ct = default)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == currentUserId && !notification.IsRead, ct);
    }

    public Task<int> MarkAllAsReadAsync(Guid currentUserId, DateTime readAtUtc, CancellationToken ct = default)
    {
        return _dbContext.Notifications
            .Where(notification => notification.UserId == currentUserId && !notification.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(notification => notification.IsRead, true)
                    .SetProperty(notification => notification.ReadAtUtc, readAtUtc),
                ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _dbContext.SaveChangesAsync(ct);
    }

    private static string? BuildDisplayName(
        string? username,
        string? name,
        string? email,
        string? externalId)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        return externalId;
    }
}
