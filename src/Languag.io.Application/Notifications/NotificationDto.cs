using Languag.io.Domain.Enums;

namespace Languag.io.Application.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid? ActorUserId,
    string? ActorDisplayName,
    string? ActorAvatarUrl,
    string? EntityType,
    Guid? EntityId,
    string? Title,
    string? Body,
    bool IsRead,
    DateTime CreatedAtUtc);
