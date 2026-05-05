using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class SagaRepositoryTests
{
    [Fact]
    public async Task CanAccessDecksAsync_AllowsOwnedAndPublicDecks()
    {
        await using var context = await CreateContextAsync();
        var repository = new SagaRepository(context);
        var currentUser = CreateUser("current");
        var otherUser = CreateUser("other");
        var ownedPrivateDeck = CreateDeck(currentUser, "Owned private", DeckVisibility.Private);
        var otherPublicDeck = CreateDeck(otherUser, "Other public", DeckVisibility.Public);
        var otherPrivateDeck = CreateDeck(otherUser, "Other private", DeckVisibility.Private);
        context.Users.AddRange(currentUser, otherUser);
        context.Decks.AddRange(ownedPrivateDeck, otherPublicDeck, otherPrivateDeck);
        await context.SaveChangesAsync();

        var canAccessVisibleDecks = await repository.CanAccessDecksAsync(
            [ownedPrivateDeck.Id, otherPublicDeck.Id, ownedPrivateDeck.Id],
            currentUser.Id);
        var canAccessOtherPrivateDeck = await repository.CanAccessDecksAsync(
            [otherPrivateDeck.Id],
            currentUser.Id);

        Assert.True(canAccessVisibleDecks);
        Assert.False(canAccessOtherPrivateDeck);
    }

    [Fact]
    public async Task AreDecksPublicAsync_RequiresAllDecksToBePublic()
    {
        await using var context = await CreateContextAsync();
        var repository = new SagaRepository(context);
        var owner = CreateUser("owner");
        var publicDeck = CreateDeck(owner, "Public", DeckVisibility.Public);
        var privateDeck = CreateDeck(owner, "Private", DeckVisibility.Private);
        context.Users.Add(owner);
        context.Decks.AddRange(publicDeck, privateDeck);
        await context.SaveChangesAsync();

        var publicOnly = await repository.AreDecksPublicAsync([publicDeck.Id]);
        var includesPrivate = await repository.AreDecksPublicAsync([publicDeck.Id, privateDeck.Id]);

        Assert.True(publicOnly);
        Assert.False(includesPrivate);
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

    private static Deck CreateDeck(User owner, string title, DeckVisibility visibility)
    {
        var now = DateTime.UtcNow;

        return new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            User = owner,
            Title = title,
            Description = $"{title} description",
            Category = "Language",
            Color = "teal",
            Visibility = visibility,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedAtUtc = now
        };
    }
}
