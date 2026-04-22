namespace Languag.io.Application.Friends;

public enum GetFriendshipStatusResultStatus
{
    Success = 1,
    NotFound = 2
}

public sealed record GetFriendshipStatusResult(
    GetFriendshipStatusResultStatus Status,
    FriendshipStatusDto? FriendshipStatus = null);
