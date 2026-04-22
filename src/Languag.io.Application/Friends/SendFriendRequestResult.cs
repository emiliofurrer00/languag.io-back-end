namespace Languag.io.Application.Friends;

public enum SendFriendRequestStatus
{
    Created = 1,
    Invalid = 2,
    TargetUserNotFound = 3,
    AlreadyFriends = 4,
    PendingRequestAlreadyExists = 5,
    ReversePendingRequestExists = 6
}

public sealed record SendFriendRequestResult(
    SendFriendRequestStatus Status,
    Guid? RequestId = null,
    string? Error = null);
