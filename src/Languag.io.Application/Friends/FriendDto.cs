namespace Languag.io.Application.Friends;

public sealed record FriendDto(
    Guid UserId,
    string? Username,
    string DisplayName,
    string? AvatarUrl,
    DateTime FriendsSinceUtc);
