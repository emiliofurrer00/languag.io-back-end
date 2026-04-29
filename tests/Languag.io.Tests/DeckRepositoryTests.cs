using Languag.io.Application.Decks;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Languag.io.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Tests;

public sealed class DeckRepositoryTests
{
    [Fact]
    public async Task GetPublicDecksAsync_FiltersByOwnerUsernameAndSearchWithoutReturningPrivateDecks()
    {
        await using var context = await CreateContextAsync();
        var repository = new DeckRepository(context);
        var ada = CreateUser("ada");
        var grace = CreateUser("grace");
        context.Users.AddRange(ada, grace);

        var matchingPublicDeck = CreateDeck(ada, "Spanish Travel", DeckVisibility.Public, updatedMinutesAgo: 1);
        var matchingPrivateDeck = CreateDeck(ada, "Spanish Private Notes", DeckVisibility.Private, updatedMinutesAgo: 2);
        var otherOwnerDeck = CreateDeck(grace, "Spanish Grammar", DeckVisibility.Public, updatedMinutesAgo: 3);
        var otherTopicDeck = CreateDeck(ada, "French Basics", DeckVisibility.Public, updatedMinutesAgo: 4);
        context.Decks.AddRange(matchingPublicDeck, matchingPrivateDeck, otherOwnerDeck, otherTopicDeck);
        await context.SaveChangesAsync();

        var decks = await repository.GetPublicDecksAsync(new DeckListQuery
        {
            Username = " ada ",
            SearchQuery = "spanish"
        });

        var deck = Assert.Single(decks);
        Assert.Equal(matchingPublicDeck.Id, deck.Id);
        Assert.Equal("ada", deck.OwnerName);
    }

    [Fact]
    public async Task GetVisibleDecksAsync_FiltersVisibleDecksBySearchAlias()
    {
        await using var context = await CreateContextAsync();
        var repository = new DeckRepository(context);
        var currentUser = CreateUser("current");
        var otherUser = CreateUser("other");
        context.Users.AddRange(currentUser, otherUser);

        var ownedPrivateDeck = CreateDeck(currentUser, "Private Spanish", DeckVisibility.Private, updatedMinutesAgo: 1);
        var publicMatchingDeck = CreateDeck(otherUser, "Public Spanish", DeckVisibility.Public, updatedMinutesAgo: 2);
        var privateOtherDeck = CreateDeck(otherUser, "Other Spanish", DeckVisibility.Private, updatedMinutesAgo: 3);
        var nonMatchingDeck = CreateDeck(currentUser, "German Basics", DeckVisibility.Public, updatedMinutesAgo: 4);
        context.Decks.AddRange(ownedPrivateDeck, publicMatchingDeck, privateOtherDeck, nonMatchingDeck);
        await context.SaveChangesAsync();

        var decks = await repository.GetVisibleDecksAsync(currentUser.Id, new DeckListQuery
        {
            Q = "spanish"
        });

        Assert.Collection(
            decks,
            deck => Assert.Equal(ownedPrivateDeck.Id, deck.Id),
            deck => Assert.Equal(publicMatchingDeck.Id, deck.Id));
    }

    [Fact]
    public async Task GetDeckByIdAsync_ReturnsMultiChoiceCardChoices()
    {
        await using var context = await CreateContextAsync();
        var repository = new DeckRepository(context);
        var owner = CreateUser("choice-maker");
        var deck = CreateDeck(owner, "Question Deck", DeckVisibility.Public, updatedMinutesAgo: 1);
        var card = deck.Cards[0];
        card.Type = CardTypes.MultiChoice;
        card.Choices =
        [
            new CardChoice
            {
                Id = Guid.NewGuid(),
                CardId = card.Id,
                Text = "hello",
                IsCorrect = true,
                Order = 0
            },
            new CardChoice
            {
                Id = Guid.NewGuid(),
                CardId = card.Id,
                Text = "goodbye",
                IsCorrect = false,
                Order = 1
            }
        ];

        context.Users.Add(owner);
        context.Decks.Add(deck);
        await context.SaveChangesAsync();

        var dto = await repository.GetDeckByIdAsync(deck.Id, owner.Id);

        Assert.NotNull(dto);
        var returnedCard = Assert.Single(dto.Cards);
        Assert.Equal(CardTypes.MultiChoice, returnedCard.Type);
        Assert.Equal("hola", returnedCard.FrontText);
        Assert.Equal("hello", returnedCard.BackText);
        Assert.Collection(
            returnedCard.Choices,
            choice =>
            {
                Assert.Equal("hello", choice.Text);
                Assert.True(choice.IsCorrect);
            },
            choice =>
            {
                Assert.Equal("goodbye", choice.Text);
                Assert.False(choice.IsCorrect);
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

    private static Deck CreateDeck(
        User owner,
        string title,
        DeckVisibility visibility,
        int updatedMinutesAgo)
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
            UpdatedAtUtc = now.AddMinutes(-updatedMinutesAgo),
            Cards =
            [
                new Card
                {
                    Id = Guid.NewGuid(),
                    FrontText = "hola",
                    BackText = "hello",
                    Order = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            ]
        };
    }
}
