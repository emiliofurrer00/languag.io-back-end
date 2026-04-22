using Languag.io.Application.Friends;
using Languag.io.Application.Notifications;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class FriendshipAndNotificationRepositoryTests
{
    [Fact]
    public async Task UniquePendingPerPairIndex_BlocksDuplicatePendingRequests()
    {
        await using var context = await CreateContextAsync();
        var (userA, userB) = await SeedTwoUsersAsync(context);
        var pair = FriendshipPair.Normalize(userA.Id, userB.Id);

        context.FriendRequests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = userA.Id,
            ReceiverId = userB.Id,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        });
        await context.SaveChangesAsync();

        context.FriendRequests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = userB.Id,
            ReceiverId = userA.Id,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1),
            PairUser1Id = pair.User1Id,
            PairUser2Id = pair.User2Id
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task NormalizedFriendshipPair_BlocksDuplicateFriendships()
    {
        await using var context = await CreateContextAsync();
        var (userA, userB) = await SeedTwoUsersAsync(context);
        var pair = FriendshipPair.Normalize(userA.Id, userB.Id);

        context.Friendships.Add(new Friendship
        {
            User1Id = pair.User1Id,
            User2Id = pair.User2Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        context.Friendships.Add(new Friendship
        {
            User1Id = pair.User1Id,
            User2Id = pair.User2Id,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task IncomingRequestsQuery_ReturnsOnlyPendingRequestsForTheReceiver()
    {
        await using var context = await CreateContextAsync();
        var repository = new FriendRequestRepository(context);
        var receiver = CreateUser("receiver");
        var sender = CreateUser("sender");
        var otherUser = CreateUser("other");
        context.Users.AddRange(receiver, sender, otherUser);
        await context.SaveChangesAsync();

        var incomingPair = FriendshipPair.Normalize(sender.Id, receiver.Id);
        var otherPair = FriendshipPair.Normalize(receiver.Id, otherUser.Id);

        context.FriendRequests.AddRange(
            new FriendRequest
            {
                Id = Guid.NewGuid(),
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                PairUser1Id = incomingPair.User1Id,
                PairUser2Id = incomingPair.User2Id
            },
            new FriendRequest
            {
                Id = Guid.NewGuid(),
                SenderId = receiver.Id,
                ReceiverId = otherUser.Id,
                Status = FriendRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                PairUser1Id = otherPair.User1Id,
                PairUser2Id = otherPair.User2Id
            },
            new FriendRequest
            {
                Id = Guid.NewGuid(),
                SenderId = otherUser.Id,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Rejected,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2),
                PairUser1Id = otherPair.User1Id,
                PairUser2Id = otherPair.User2Id
            });
        await context.SaveChangesAsync();

        var page = await repository.GetIncomingPendingAsync(receiver.Id, new GetIncomingFriendRequestsQuery(), CancellationToken.None);

        var incoming = Assert.Single(page.Items);
        Assert.Equal(sender.Id, incoming.SenderId);
        Assert.Equal(receiver.Id, incoming.ReceiverId);
        Assert.Equal(FriendRequestStatus.Pending, incoming.Status);
    }

    [Fact]
    public async Task UnreadNotificationCount_MatchesUnreadRows()
    {
        await using var context = await CreateContextAsync();
        var repository = new NotificationRepository(context);
        var user = CreateUser("user");
        var otherUser = CreateUser("other");
        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();

        context.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = NotificationType.FriendRequestReceived,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = NotificationType.FriendRequestAccepted,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = NotificationType.FriendRequestAccepted,
                IsRead = true,
                ReadAtUtc = DateTime.UtcNow.AddMinutes(-1),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = otherUser.Id,
                Type = NotificationType.FriendRequestReceived,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
            });
        await context.SaveChangesAsync();

        var unreadCount = await repository.GetUnreadCountAsync(user.Id);

        Assert.Equal(2, unreadCount);
    }

    [Fact]
    public async Task MarkAllRead_UpdatesOnlyCurrentUsersUnreadNotifications()
    {
        await using var context = await CreateContextAsync();
        var repository = new NotificationRepository(context);
        var user = CreateUser("user");
        var otherUser = CreateUser("other");
        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();

        context.Notifications.AddRange(
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = NotificationType.FriendRequestReceived,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Type = NotificationType.FriendRequestAccepted,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            },
            new Notification
            {
                Id = Guid.NewGuid(),
                UserId = otherUser.Id,
                Type = NotificationType.FriendRequestReceived,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            });
        await context.SaveChangesAsync();

        var updated = await repository.MarkAllAsReadAsync(user.Id, DateTime.UtcNow);
        var userUnreadCount = await context.Notifications.CountAsync(notification => notification.UserId == user.Id && !notification.IsRead);
        var otherUnreadCount = await context.Notifications.CountAsync(notification => notification.UserId == otherUser.Id && !notification.IsRead);

        Assert.Equal(2, updated);
        Assert.Equal(0, userUnreadCount);
        Assert.Equal(1, otherUnreadCount);
    }

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<(User First, User Second)> SeedTwoUsersAsync(AppDbContext context)
    {
        var first = CreateUser("first");
        var second = CreateUser("second");
        context.Users.AddRange(first, second);
        await context.SaveChangesAsync();
        return (first, second);
    }

    private static User CreateUser(string suffix)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = $"kp_{suffix}_{Guid.NewGuid():N}",
            Email = $"{suffix}@example.com",
            Name = $"{suffix} user",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
