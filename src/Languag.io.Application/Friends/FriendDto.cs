namespace Languag.io.Application.Friends;

public sealed record FriendDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    DateTime FriendsSinceUtc);
