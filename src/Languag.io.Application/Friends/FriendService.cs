using Languag.io.Application.Common;
using Languag.io.Application.Notifications;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;

namespace Languag.io.Application.Friends;

public sealed class FriendService : IFriendService
{
    private const string FriendRequestEntityType = "FriendRequest";

    private readonly IFriendRequestRepository _friendRequestRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly INotificationRepository _notificationRepository;

    public FriendService(
        IFriendRequestRepository friendRequestRepository,
        IFriendshipRepository friendshipRepository,
        INotificationRepository notificationRepository)
    {
        _friendRequestRepository = friendRequestRepository;
        _friendshipRepository = friendshipRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task<SendFriendRequestResult> SendFriendRequestAsync(
        SendFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        if (command.TargetUserId == currentUserId)
        {
            return new SendFriendRequestResult(
                SendFriendRequestStatus.Invalid,
                Error: "You cannot send a friend request to yourself.");
        }

        if (!await _friendRequestRepository.UserExistsAsync(command.TargetUserId, ct))
        {
            return new SendFriendRequestResult(SendFriendRequestStatus.TargetUserNotFound);
        }

        var pair = FriendshipPair.Normalize(currentUserId, command.TargetUserId);
        if (await _friendshipRepository.ExistsAsync(pair.User1Id, pair.User2Id, ct))
        {
            return new SendFriendRequestResult(
                SendFriendRequestStatus.AlreadyFriends,
                Error: "You are already friends with that user.");
        }

        var existingPendingRequest = await _friendRequestRepository.GetPendingByPairAsync(
            pair.User1Id,
            pair.User2Id,
            ct);

        if (existingPendingRequest is not null)
        {
            return existingPendingRequest.SenderId == currentUserId
                ? new SendFriendRequestResult(
                    SendFriendRequestStatus.PendingRequestAlreadyExists,
                    Error: "A pending friend request already exists for this pair.")
                : new SendFriendRequestResult(
                    SendFriendRequestStatus.ReversePendingRequestExists,
                    Error: "This user has already sent you a friend request.");
        }

        var now = DateTime.UtcNow;
        var request = new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = command.TargetUserId,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = now,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        };

        await _friendRequestRepository.AddAsync(request, ct);
        await _notificationRepository.AddAsync(
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = command.TargetUserId,
                Type = NotificationType.FriendRequestReceived,
                ActorUserId = currentUserId,
                EntityType = FriendRequestEntityType,
                EntityId = request.Id,
                Title = "New friend request",
                Body = "You received a new friend request.",
                CreatedAtUtc = now
            },
            ct);

        await _friendRequestRepository.SaveChangesAsync(ct);

        return new SendFriendRequestResult(SendFriendRequestStatus.Created, request.Id);
    }

    public async Task<FriendRequestCommandResult> AcceptFriendRequestAsync(
        AcceptFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var request = await _friendRequestRepository.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return new FriendRequestCommandResult(FriendRequestCommandStatus.NotFound);
        }

        if (request.ReceiverId != currentUserId)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Forbidden,
                "Only the receiver can accept this friend request.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Conflict,
                "Only pending friend requests can be accepted.");
        }

        var pair = FriendshipPair.Normalize(request.SenderId, request.ReceiverId);
        if (await _friendshipRepository.ExistsAsync(pair.User1Id, pair.User2Id, ct))
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Conflict,
                "These users are already friends.");
        }

        var now = DateTime.UtcNow;
        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAtUtc = now;

        await _friendshipRepository.AddAsync(
            new Friendship
            {
                User1Id = pair.User1Id,
                User2Id = pair.User2Id,
                CreatedAtUtc = now,
                CreatedFromRequestId = request.Id
            },
            ct);

        await _notificationRepository.AddAsync(
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.SenderId,
                Type = NotificationType.FriendRequestAccepted,
                ActorUserId = request.ReceiverId,
                EntityType = FriendRequestEntityType,
                EntityId = request.Id,
                Title = "Friend request accepted",
                Body = "Your friend request was accepted.",
                CreatedAtUtc = now
            },
            ct);

        await _friendRequestRepository.SaveChangesAsync(ct);

        return new FriendRequestCommandResult(FriendRequestCommandStatus.Success);
    }

    public async Task<FriendRequestCommandResult> RejectFriendRequestAsync(
        RejectFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var request = await _friendRequestRepository.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return new FriendRequestCommandResult(FriendRequestCommandStatus.NotFound);
        }

        if (request.ReceiverId != currentUserId)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Forbidden,
                "Only the receiver can reject this friend request.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Conflict,
                "Only pending friend requests can be rejected.");
        }

        request.Status = FriendRequestStatus.Rejected;
        request.RespondedAtUtc = DateTime.UtcNow;
        await _friendRequestRepository.SaveChangesAsync(ct);

        return new FriendRequestCommandResult(FriendRequestCommandStatus.Success);
    }

    public async Task<FriendRequestCommandResult> CancelFriendRequestAsync(
        CancelFriendRequestCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var request = await _friendRequestRepository.GetByIdAsync(command.RequestId, ct);
        if (request is null)
        {
            return new FriendRequestCommandResult(FriendRequestCommandStatus.NotFound);
        }

        if (request.SenderId != currentUserId)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Forbidden,
                "Only the sender can cancel this friend request.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            return new FriendRequestCommandResult(
                FriendRequestCommandStatus.Conflict,
                "Only pending friend requests can be cancelled.");
        }

        request.Status = FriendRequestStatus.Cancelled;
        request.RespondedAtUtc = DateTime.UtcNow;
        await _friendRequestRepository.SaveChangesAsync(ct);

        return new FriendRequestCommandResult(FriendRequestCommandStatus.Success);
    }

    public async Task<RemoveFriendResult> RemoveFriendAsync(
        RemoveFriendCommand command,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        var pair = FriendshipPair.Normalize(currentUserId, command.FriendUserId);
        var friendship = await _friendshipRepository.GetByPairAsync(pair.User1Id, pair.User2Id, ct);
        if (friendship is null)
        {
            return new RemoveFriendResult(RemoveFriendStatus.NotFound);
        }

        _friendshipRepository.Remove(friendship);
        await _friendshipRepository.SaveChangesAsync(ct);

        return new RemoveFriendResult(RemoveFriendStatus.Success);
    }

    public Task<CursorPage<FriendRequestDto>> GetIncomingFriendRequestsAsync(
        GetIncomingFriendRequestsQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        return _friendRequestRepository.GetIncomingPendingAsync(currentUserId, query, ct);
    }

    public Task<CursorPage<FriendRequestDto>> GetOutgoingFriendRequestsAsync(
        GetOutgoingFriendRequestsQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        return _friendRequestRepository.GetOutgoingPendingAsync(currentUserId, query, ct);
    }

    public Task<CursorPage<FriendDto>> GetFriendsAsync(
        GetFriendsQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        return _friendshipRepository.GetFriendsAsync(currentUserId, query, ct);
    }

    public async Task<GetFriendshipStatusResult> GetFriendshipStatusAsync(
        GetFriendshipStatusQuery query,
        Guid currentUserId,
        CancellationToken ct = default)
    {
        if (!await _friendRequestRepository.UserExistsAsync(query.OtherUserId, ct))
        {
            return new GetFriendshipStatusResult(GetFriendshipStatusResultStatus.NotFound);
        }

        if (query.OtherUserId == currentUserId)
        {
            return new GetFriendshipStatusResult(
                GetFriendshipStatusResultStatus.Success,
                new FriendshipStatusDto(FriendshipStatuses.None));
        }

        var pair = FriendshipPair.Normalize(currentUserId, query.OtherUserId);
        if (await _friendshipRepository.ExistsAsync(pair.User1Id, pair.User2Id, ct))
        {
            return new GetFriendshipStatusResult(
                GetFriendshipStatusResultStatus.Success,
                new FriendshipStatusDto(FriendshipStatuses.Friends));
        }

        var pendingRequest = await _friendRequestRepository.GetPendingByPairAsync(pair.User1Id, pair.User2Id, ct);
        if (pendingRequest is null)
        {
            return new GetFriendshipStatusResult(
                GetFriendshipStatusResultStatus.Success,
                new FriendshipStatusDto(FriendshipStatuses.None));
        }

        var status = pendingRequest.ReceiverId == currentUserId
            ? FriendshipStatuses.IncomingPending
            : FriendshipStatuses.OutgoingPending;

        return new GetFriendshipStatusResult(
            GetFriendshipStatusResultStatus.Success,
            new FriendshipStatusDto(status));
    }
}
