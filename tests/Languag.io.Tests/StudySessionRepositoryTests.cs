using Languag.io.Application.StudySessions;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class StudySessionRepositoryTests
{
    [Fact]
    public async Task GetDeckStudyPlanAsync_OrdersDueLapsedNewAndFutureCards()
    {
        await using var context = await CreateContextAsync();
        var repository = new StudySessionRepository(context);
        var now = DateTime.UtcNow;
        var user = CreateUser("study");
        var deck = CreateDeck(user);
        var dueCard = CreateCard(deck, "due", 0);
        var lapsedCard = CreateCard(deck, "lapsed", 1);
        var newCard = CreateCard(deck, "new", 2);
        var futureCard = CreateCard(deck, "future", 3);
        context.Users.Add(user);
        context.Decks.Add(deck);
        context.Cards.AddRange(dueCard, lapsedCard, newCard, futureCard);
        context.CardReviewStates.AddRange(
            new CardReviewState
            {
                UserId = user.Id,
                DeckId = deck.Id,
                CardId = dueCard.Id,
                DueAtUtc = now.AddHours(-1),
                LastReviewedAtUtc = now.AddDays(-2),
                IntervalDays = 1,
                EaseFactor = 2.5m,
                RepetitionCount = 1,
                TotalReviews = 1,
                CorrectReviews = 1
            },
            new CardReviewState
            {
                UserId = user.Id,
                DeckId = deck.Id,
                CardId = lapsedCard.Id,
                DueAtUtc = now.AddDays(1),
                LastReviewedAtUtc = now,
                IntervalDays = 1,
                EaseFactor = 2.3m,
                RepetitionCount = 0,
                LapseCount = 1,
                TotalReviews = 2,
                CorrectReviews = 1
            },
            new CardReviewState
            {
                UserId = user.Id,
                DeckId = deck.Id,
                CardId = futureCard.Id,
                DueAtUtc = now.AddDays(5),
                LastReviewedAtUtc = now,
                IntervalDays = 5,
                EaseFactor = 2.55m,
                RepetitionCount = 2,
                TotalReviews = 2,
                CorrectReviews = 2
            });
        await context.SaveChangesAsync();

        var studyPlan = await repository.GetDeckStudyPlanAsync(deck.Id, user.Id, now, 10);

        Assert.NotNull(studyPlan);
        Assert.Collection(
            studyPlan,
            card =>
            {
                Assert.Equal(dueCard.Id, card.CardId);
                Assert.Equal("Due", card.Reason);
                Assert.True(card.IsDue);
            },
            card =>
            {
                Assert.Equal(lapsedCard.Id, card.CardId);
                Assert.Equal("Lapsed", card.Reason);
            },
            card =>
            {
                Assert.Equal(newCard.Id, card.CardId);
                Assert.Equal("New", card.Reason);
                Assert.True(card.IsNew);
            },
            card =>
            {
                Assert.Equal(futureCard.Id, card.CardId);
                Assert.Equal("Review", card.Reason);
            });
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

    private static User CreateUser(string username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = $"kp_{username}_{Guid.NewGuid():N}",
            Username = username,
            Email = $"{username}@example.com",
            Name = $"{username} user",
            AvatarColor = "teal",
            ProfileDescription = string.Empty,
            About = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Deck CreateDeck(User owner)
    {
        return new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            User = owner,
            Title = "Spanish Basics",
            Description = "Starter cards",
            Category = "Spanish",
            Color = "teal",
            Visibility = DeckVisibility.Private,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Card CreateCard(Deck deck, string frontText, int order)
    {
        return new Card
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            Deck = deck,
            FrontText = frontText,
            BackText = $"{frontText} back",
            Order = order,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
