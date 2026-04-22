namespace Languag.io.Application.Friends;

public enum RemoveFriendStatus
{
    Success = 1,
    NotFound = 2
}

public sealed record RemoveFriendResult(RemoveFriendStatus Status);
