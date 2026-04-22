using Languag.io.Application.Common;

namespace Languag.io.Application.Friends;

public interface IFriendService
{
    Task<SendFriendRequestResult> SendFriendRequestAsync(
        SendFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<FriendRequestCommandResult> AcceptFriendRequestAsync(
        AcceptFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<FriendRequestCommandResult> RejectFriendRequestAsync(
        RejectFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<FriendRequestCommandResult> CancelFriendRequestAsync(
        CancelFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<RemoveFriendResult> RemoveFriendAsync(
        RemoveFriendCommand command,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<CursorPage<FriendRequestDto>> GetIncomingFriendRequestsAsync(
        GetIncomingFriendRequestsQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<CursorPage<FriendRequestDto>> GetOutgoingFriendRequestsAsync(
        GetOutgoingFriendRequestsQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<CursorPage<FriendDto>> GetFriendsAsync(
        GetFriendsQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
    Task<GetFriendshipStatusResult> GetFriendshipStatusAsync(
        GetFriendshipStatusQuery query,
        Guid currentUserId,
        CancellationToken ct = default);
}
