using Languag.io.Application.Common;
using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;

namespace Languag.io.Tests;

public sealed class FriendAndNotificationServiceTests
{
    [Fact]
    public async Task SendFriendRequestAsync_ReturnsInvalid_WhenSendingToSelf()
    {
        var currentUserId = Guid.NewGuid();
        var friendRequestRepository = new FakeFriendRequestRepository();
        var friendshipRepository = new FakeFriendshipRepository();
        var notificationRepository = new FakeNotificationRepository();
        var service = new FriendService(friendRequestRepository, friendshipRepository, notificationRepository);

        var result = await service.SendFriendRequestAsync(new SendFriendRequestCommand(currentUserId), currentUserId);

        Assert.Equal(SendFriendRequestStatus.Invalid, result.Status);
        Assert.Empty(friendRequestRepository.Requests);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ReturnsAlreadyFriends_WhenUsersAreAlreadyFriends()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(currentUserId, targetUserId);
        var friendRequestRepository = new FakeFriendRequestRepository(targetUserId);
        var friendshipRepository = new FakeFriendshipRepository();
        var notificationRepository = new FakeNotificationRepository();
        friendshipRepository.Friendships.Add(new Friendship
        {
            User1Id = pair.User1Id,
            User2Id = pair.User2Id,
            CreatedAtUtc = DateTime.UtcNow
        });

        var service = new FriendService(friendRequestRepository, friendshipRepository, notificationRepository);

        var result = await service.SendFriendRequestAsync(new SendFriendRequestCommand(targetUserId), currentUserId);

        Assert.Equal(SendFriendRequestStatus.AlreadyFriends, result.Status);
        Assert.Empty(friendRequestRepository.Requests);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task SendFriendRequestAsync_ReturnsPendingConflict_WhenDuplicatePendingRequestExists()
    {
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(currentUserId, targetUserId);
        var friendRequestRepository = new FakeFriendRequestRepository(targetUserId);
        friendRequestRepository.Requests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = targetUserId,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });

        var service = new FriendService(friendRequestRepository, new FakeFriendshipRepository(), new FakeNotificationRepository());

        var result = await service.SendFriendRequestAsync(new SendFriendRequestCommand(targetUserId), currentUserId);

        Assert.Equal(SendFriendRequestStatus.PendingRequestAlreadyExists, result.Status);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_ReturnsConflict_WhenRequestIsNotPending()
    {
        var currentUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var friendRequestRepository = new FakeFriendRequestRepository(currentUserId, senderId);
        friendRequestRepository.Requests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = currentUserId,
            Status = FriendRequestStatus.Rejected,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = FriendshipPair.Normalize(currentUserId, senderId).User1Id,
            PairUser2Id = FriendshipPair.Normalize(currentUserId, senderId).User2Id
        });

        var service = new FriendService(friendRequestRepository, new FakeFriendshipRepository(), new FakeNotificationRepository());

        var result = await service.AcceptFriendRequestAsync(
            new AcceptFriendRequestCommand(friendRequestRepository.Requests[0].Id),
            currentUserId);

        Assert.Equal(FriendRequestCommandStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_ReturnsForbidden_WhenCurrentUserIsNotReceiver()
    {
        var currentUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var actualReceiverId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(senderId, actualReceiverId);
        var friendRequestRepository = new FakeFriendRequestRepository(currentUserId, senderId, actualReceiverId);
        friendRequestRepository.Requests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = actualReceiverId,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        });

        var service = new FriendService(friendRequestRepository, new FakeFriendshipRepository(), new FakeNotificationRepository());

        var result = await service.AcceptFriendRequestAsync(
            new AcceptFriendRequestCommand(friendRequestRepository.Requests[0].Id),
            currentUserId);

        Assert.Equal(FriendRequestCommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_CreatesFriendshipAndNotification()
    {
        var receiverId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(receiverId, senderId);
        var friendRequestRepository = new FakeFriendRequestRepository(receiverId, senderId);
        var friendshipRepository = new FakeFriendshipRepository();
        var notificationRepository = new FakeNotificationRepository();
        var request = new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        };
        friendRequestRepository.Requests.Add(request);

        var service = new FriendService(friendRequestRepository, friendshipRepository, notificationRepository);

        var result = await service.AcceptFriendRequestAsync(new AcceptFriendRequestCommand(request.Id), receiverId);

        Assert.Equal(FriendRequestCommandStatus.Success, result.Status);
        Assert.Equal(FriendRequestStatus.Accepted, request.Status);
        Assert.NotNull(request.RespondedAtUtc);
        var friendship = Assert.Single(friendshipRepository.Friendships);
        Assert.Equal(pair.User1Id, friendship.User1Id);
        Assert.Equal(pair.User2Id, friendship.User2Id);
        Assert.Equal(request.Id, friendship.CreatedFromRequestId);
        var notification = Assert.Single(notificationRepository.Notifications);
        Assert.Equal(senderId, notification.UserId);
        Assert.Equal(receiverId, notification.ActorUserId);
        Assert.Equal(NotificationType.FriendRequestAccepted, notification.Type);
        Assert.True(friendRequestRepository.SaveChangesCalled);
    }

    [Fact]
    public async Task CancelFriendRequestAsync_ReturnsForbidden_WhenCurrentUserIsNotSender()
    {
        var currentUserId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(currentUserId, senderId);
        var friendRequestRepository = new FakeFriendRequestRepository(currentUserId, senderId);
        friendRequestRepository.Requests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = currentUserId,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        });

        var service = new FriendService(friendRequestRepository, new FakeFriendshipRepository(), new FakeNotificationRepository());

        var result = await service.CancelFriendRequestAsync(
            new CancelFriendRequestCommand(friendRequestRepository.Requests[0].Id),
            currentUserId);

        Assert.Equal(FriendRequestCommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task RejectFriendRequestAsync_ReturnsForbidden_WhenCurrentUserIsNotReceiver()
    {
        var currentUserId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var pair = FriendshipPair.Normalize(currentUserId, receiverId);
        var friendRequestRepository = new FakeFriendRequestRepository(currentUserId, receiverId);
        friendRequestRepository.Requests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = currentUserId,
            ReceiverId = receiverId,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        });

        var service = new FriendService(friendRequestRepository, new FakeFriendshipRepository(), new FakeNotificationRepository());

        var result = await service.RejectFriendRequestAsync(
            new RejectFriendRequestCommand(friendRequestRepository.Requests[0].Id),
            currentUserId);

        Assert.Equal(FriendRequestCommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_IsIdempotent()
    {
        var currentUserId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = currentUserId,
            Type = NotificationType.FriendRequestReceived,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        var repository = new FakeNotificationRepository(notification);
        var service = new NotificationService(repository);

        var first = await service.MarkNotificationReadAsync(new MarkNotificationReadCommand(notification.Id), currentUserId);
        var second = await service.MarkNotificationReadAsync(new MarkNotificationReadCommand(notification.Id), currentUserId);

        Assert.Equal(MarkNotificationReadStatus.Success, first.Status);
        Assert.Equal(MarkNotificationReadStatus.Success, second.Status);
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAtUtc);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    private sealed class FakeFriendRequestRepository : IFriendRequestRepository
    {
        private readonly HashSet<Guid> _existingUserIds;

        public FakeFriendRequestRepository(params Guid[] existingUserIds)
        {
            _existingUserIds = existingUserIds.ToHashSet();
        }

        public List<FriendRequest> Requests { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult(_existingUserIds.Contains(userId));
        }

        public Task<FriendRequest?> GetByIdAsync(Guid requestId, CancellationToken ct = default)
        {
            return Task.FromResult(Requests.SingleOrDefault(request => request.Id == requestId));
        }

        public Task<FriendRequest?> GetPendingByPairAsync(Guid pairUser1Id, Guid pairUser2Id, CancellationToken ct = default)
        {
            return Task.FromResult(Requests.SingleOrDefault(request =>
                request.PairUser1Id == pairUser1Id
                && request.PairUser2Id == pairUser2Id
                && request.Status == FriendRequestStatus.Pending));
        }

        public Task AddAsync(FriendRequest friendRequest, CancellationToken ct = default)
        {
            Requests.Add(friendRequest);
            return Task.CompletedTask;
        }

        public Task<CursorPage<FriendRequestDto>> GetIncomingPendingAsync(
            Guid currentUserId,
            GetIncomingFriendRequestsQuery query,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<CursorPage<FriendRequestDto>> GetOutgoingPendingAsync(
            Guid currentUserId,
            GetOutgoingFriendRequestsQuery query,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFriendshipRepository : IFriendshipRepository
    {
        public List<Friendship> Friendships { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
        {
            return Task.FromResult(Friendships.Any(friendship => friendship.User1Id == user1Id && friendship.User2Id == user2Id));
        }

        public Task<Friendship?> GetByPairAsync(Guid user1Id, Guid user2Id, CancellationToken ct = default)
        {
            return Task.FromResult(Friendships.SingleOrDefault(friendship => friendship.User1Id == user1Id && friendship.User2Id == user2Id));
        }

        public Task AddAsync(Friendship friendship, CancellationToken ct = default)
        {
            Friendships.Add(friendship);
            return Task.CompletedTask;
        }

        public void Remove(Friendship friendship)
        {
            Friendships.Remove(friendship);
        }

        public Task<CursorPage<FriendDto>> GetFriendsAsync(Guid currentUserId, GetFriendsQuery query, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public FakeNotificationRepository(params Notification[] notifications)
        {
            Notifications.AddRange(notifications);
        }

        public List<Notification> Notifications { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public Task AddAsync(Notification notification, CancellationToken ct = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken ct = default)
        {
            return Task.FromResult(Notifications.SingleOrDefault(notification => notification.Id == notificationId));
        }

        public Task<CursorPage<NotificationDto>> GetNotificationsAsync(Guid currentUserId, GetNotificationsQuery query, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken ct = default)
        {
            return Task.FromResult(Notifications.Count(notification => notification.UserId == currentUserId && !notification.IsRead));
        }

        public Task<int> MarkAllAsReadAsync(Guid currentUserId, DateTime readAtUtc, CancellationToken ct = default)
        {
            var updated = 0;
            foreach (var notification in Notifications.Where(notification => notification.UserId == currentUserId && !notification.IsRead))
            {
                notification.IsRead = true;
                notification.ReadAtUtc = readAtUtc;
                updated++;
            }

            return Task.FromResult(updated);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
