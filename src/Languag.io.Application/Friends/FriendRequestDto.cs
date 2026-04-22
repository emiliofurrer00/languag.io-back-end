using Languag.io.Domain.Enums;

namespace Languag.io.Application.Friends;

public sealed record FriendRequestDto(
    Guid Id,
    Guid SenderId,
    string SenderDisplayName,
    string? SenderAvatarUrl,
    Guid ReceiverId,
    string ReceiverDisplayName,
    string? ReceiverAvatarUrl,
    FriendRequestStatus Status,
    DateTime CreatedAtUtc);
