namespace Languag.io.Application.Friends;

public enum FriendRequestCommandStatus
{
    Success = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4
}

public sealed record FriendRequestCommandResult(
    FriendRequestCommandStatus Status,
    string? Error = null);
