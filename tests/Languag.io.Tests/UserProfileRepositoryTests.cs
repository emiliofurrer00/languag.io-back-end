using Languag.io.Application.Users;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class UserProfileRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_CalculatesStudyStreakInUserTimeZone()
    {
        await using var context = await CreateContextAsync();
        var now = new DateTime(2026, 5, 10, 2, 0, 0, DateTimeKind.Utc);
        var repository = new UserProfileRepository(
            context,
            new StubProfilePictureUrlBuilder(),
            new TestClock(now));

        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = $"kp_{Guid.NewGuid():N}",
            Username = "currentuser",
            Email = "current@example.com",
            Name = "Current User",
            TimeZoneId = "America/Buenos_Aires",
            AvatarColor = "teal",
            ProfileDescription = string.Empty,
            About = string.Empty,
            CreatedAtUtc = now.AddDays(-10),
            UpdatedAtUtc = now.AddDays(-1)
        };
        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = user.Id,
            Title = "Spanish Basics",
            Category = "Language",
            Color = "teal",
            Visibility = DeckVisibility.Private,
            CreatedAtUtc = now.AddDays(-3),
            UpdatedAtUtc = now.AddDays(-1)
        };
        var card = new Card
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            FrontText = "hola",
            BackText = "hello",
            Order = 0,
            CreatedAtUtc = now.AddDays(-3),
            UpdatedAtUtc = now.AddDays(-3)
        };
        deck.Cards.Add(card);

        var todayLocalSession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            UserId = user.Id,
            CreatedAtUtc = new DateTime(2026, 5, 10, 1, 0, 0, DateTimeKind.Utc),
            PercentageCorrect = 100m
        };
        var yesterdayLocalSession = new StudySession
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            UserId = user.Id,
            CreatedAtUtc = new DateTime(2026, 5, 9, 1, 0, 0, DateTimeKind.Utc),
            PercentageCorrect = 0m
        };

        context.Users.Add(user);
        context.Decks.Add(deck);
        context.StudySessions.AddRange(todayLocalSession, yesterdayLocalSession);
        context.StudySessionResponses.Add(new StudySessionResponse
        {
            Id = Guid.NewGuid(),
            StudySessionId = todayLocalSession.Id,
            DeckId = deck.Id,
            CardId = card.Id,
            UserId = user.Id,
            WasCorrect = true
        });
        await context.SaveChangesAsync();

        var profile = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal("America/Buenos_Aires", profile!.TimeZoneId);
        Assert.NotNull(profile.Stats);
        Assert.Equal(1, profile.Stats!.CardsStudied);
        Assert.Equal(1, profile.Stats.MasteredDecks);
        Assert.Equal(2, profile.Stats.StudyStreakDays);
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

    private sealed class StubProfilePictureUrlBuilder : IProfilePictureUrlBuilder
    {
        public string? BuildPublicUrl(string? objectKey)
        {
            return string.IsNullOrWhiteSpace(objectKey)
                ? null
                : $"https://cdn.example.test/{objectKey}";
        }
    }
}
