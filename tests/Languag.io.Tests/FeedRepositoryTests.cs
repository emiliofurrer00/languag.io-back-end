using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class FeedRepositoryTests
{
    [Fact]
    public async Task GetFeedAsync_BuildsFeedFromPersistedData()
    {
        await using var context = await CreateContextAsync();
        var repository = new FeedRepository(context);
        var now = DateTime.UtcNow;

        var currentUser = CreateUser("current", "currentuser")
        {
            DailyCardsGoal = 20,
            IsPublicProfile = true
        };
        var friend = CreateUser("friend", "friendly")
        {
            Name = "Friendly Learner",
            AvatarColor = "magenta",
            IsPublicProfile = true,
            ProfileDescription = "Spanish learner"
        };
        var pendingUser = CreateUser("pending", "pendingpal")
        {
            IsPublicProfile = true
        };
        var suggestedUser = CreateUser("suggested", "suggestedone")
        {
            Name = "Suggested Person",
            AvatarColor = "blue",
            IsPublicProfile = true,
            ProfileDescription = "Enjoys public decks"
        };

        context.Users.AddRange(currentUser, friend, pendingUser, suggestedUser);
        await context.SaveChangesAsync();

        var friendshipPair = FriendshipPair.Normalize(currentUser.Id, friend.Id);
        context.Friendships.Add(new Friendship
        {
            User1Id = friendshipPair.User1Id,
            User2Id = friendshipPair.User2Id,
            CreatedAtUtc = now.AddDays(-5)
        });

        var pendingPair = FriendshipPair.Normalize(currentUser.Id, pendingUser.Id);
        context.FriendRequests.Add(new FriendRequest
        {
            Id = Guid.NewGuid(),
            SenderId = currentUser.Id,
            ReceiverId = pendingUser.Id,
            Status = FriendRequestStatus.Pending,
            CreatedAtUtc = now.AddDays(-1),
            PairUser1Id = pendingPair.User1Id,
            PairUser2Id = pendingPair.User2Id
        });

        var ownedDeck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = currentUser.Id,
            Title = "React Fundamentals",
            Category = "Programming",
            Color = "yellow",
            Visibility = DeckVisibility.Public,
            CreatedAtUtc = now.AddDays(-10),
            UpdatedAtUtc = now.AddHours(-3)
        };
        var friendDeck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = friend.Id,
            Title = "Spanish Basics",
            Category = "Language",
            Color = "coral",
            Visibility = DeckVisibility.Public,
            CreatedAtUtc = now.AddDays(-8),
            UpdatedAtUtc = now.AddHours(-1)
        };

        ownedDeck.Cards.AddRange(
        [
            new Card
            {
                Id = Guid.NewGuid(),
                FrontText = "A",
                BackText = "A",
                Order = 0
            },
            new Card
            {
                Id = Guid.NewGuid(),
                FrontText = "B",
                BackText = "B",
                Order = 1
            }
        ]);
        friendDeck.Cards.Add(new Card
        {
            Id = Guid.NewGuid(),
            FrontText = "Hola",
            BackText = "Hello",
            Order = 0
        });

        context.Decks.AddRange(ownedDeck, friendDeck);

        var studySession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = ownedDeck.Id,
            UserId = currentUser.Id,
            CreatedAtUtc = now.AddHours(-2),
            PercentageCorrect = 100m
        };

        context.StudySessions.Add(studySession);
        context.StudySessionResponses.AddRange(
            new StudySessionResponse
            {
                Id = Guid.NewGuid(),
                StudySessionId = studySession.Id,
                DeckId = ownedDeck.Id,
                CardId = ownedDeck.Cards[0].Id,
                UserId = currentUser.Id,
                WasCorrect = true
            },
            new StudySessionResponse
            {
                Id = Guid.NewGuid(),
                StudySessionId = studySession.Id,
                DeckId = ownedDeck.Id,
                CardId = ownedDeck.Cards[1].Id,
                UserId = currentUser.Id,
                WasCorrect = true
            });

        context.ActivityLogs.Add(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = friend.Id,
            DeckId = friendDeck.Id,
            Type = ActivityType.DeckMastered,
            OccurredAtUtc = now.AddHours(-1),
            CreatedAtUtc = now.AddHours(-1)
        });

        await context.SaveChangesAsync();

        var feed = await repository.GetFeedAsync(currentUser.Id, CancellationToken.None);

        Assert.NotNull(feed);
        Assert.Equal(20, feed!.DailyGoal.Goal);
        Assert.Equal(2, feed.DailyGoal.Progress);
        Assert.Equal(1, feed.Summary.Decks);
        Assert.Equal(2, feed.Summary.Cards);

        var continueDeck = Assert.Single(feed.ContinueStudying);
        Assert.Equal(ownedDeck.Id, continueDeck.Id);
        Assert.Equal(100, continueDeck.Progress);

        var activity = Assert.Single(feed.FriendsActivity);
        Assert.Equal(friend.Id, activity.UserId);
        Assert.Equal("mastered", activity.Action);
        Assert.Equal("Spanish Basics", activity.Target);
        Assert.True(activity.FollowsYou);
        Assert.True(activity.IsFollowing);

        var suggestedPerson = Assert.Single(feed.SuggestedPeople);
        Assert.Equal(suggestedUser.Id, suggestedPerson.UserId);
        Assert.Equal(FriendshipStatuses.None, suggestedPerson.FriendshipStatus);

        var suggestedDeck = Assert.Single(feed.SuggestedDecks);
        Assert.Equal(friendDeck.Id, suggestedDeck.Id);
        Assert.Equal("Spanish Basics", suggestedDeck.Title);
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

    private static User CreateUser(string suffix, string username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = $"kp_{suffix}_{Guid.NewGuid():N}",
            Username = username,
            Email = $"{suffix}@example.com",
            Name = $"{suffix} user",
            AvatarColor = "teal",
            ProfileDescription = string.Empty,
            About = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
