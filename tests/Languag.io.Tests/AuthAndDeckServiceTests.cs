using System.Security.Claims;
using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Application.Decks;
using Languag.io.Domain.Entities;

namespace Languag.io.Tests;

public class AuthAndDeckServiceTests
{
    [Fact]
    public void ToAuthenticatedUser_MapsKindeClaimsIntoApplicationUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "kp_123"),
            new Claim("email", "ada@example.com"),
            new Claim("given_name", "Ada"),
            new Claim("family_name", "Lovelace")
        ],
        authenticationType: "Bearer"));

        var user = principal.ToAuthenticatedUser();

        Assert.NotNull(user);
        Assert.Equal("kp_123", user!.ExternalId);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("Ada Lovelace", user.Name);
    }

    [Fact]
    public async Task CreateDeckAsync_AssignsOwnerAndPreservesCardFields()
    {
        var repository = new CapturingDeckRepository();
        var service = new DeckService(repository);
        var ownerId = Guid.NewGuid();

        var deckId = await service.CreateDeckAsync(
            new CreateDeckCommand(
                "Spanish Basics",
                "Common starter words",
                "Spanish",
                "teal",
                DeckVisibility.Private,
                [
                    new Card
                    {
                        FrontText = "hola",
                        BackText = "hello",
                        ExampleSentence = "Hola, que tal?",
                        Order = 3
                    }
                ]),
            ownerId);

        Assert.Equal(deckId, repository.AddedDeck!.Id);
        Assert.Equal(ownerId, repository.AddedDeck.OwnerId);
        Assert.Single(repository.AddedDeck.Cards);
        Assert.Equal("Hola, que tal?", repository.AddedDeck.Cards[0].ExampleSentence);
        Assert.Equal(3, repository.AddedDeck.Cards[0].Order);
        Assert.Equal(repository.AddedDeck.Id, repository.AddedDeck.Cards[0].DeckId);
        Assert.True(repository.SaveChangesCalled);
    }

    private sealed class CapturingDeckRepository : IDeckRepository
    {
        public Deck? AddedDeck { get; private set; }
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<DeckDto>> GetVisibleDecksAsync(Guid ownerId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Deck deck, CancellationToken ct = default)
        {
            AddedDeck = deck;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<DeckDto?> GetDeckByIdAsync(Guid deckId, Guid? ownerId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<Deck?> GetDeckByIdForUpdateAsync(Guid deckId, Guid ownerId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public void RemoveCards(IEnumerable<Card> cards)
        {
            throw new NotImplementedException();
        }

        public Task DeleteCardsByDeckIdAsync(Guid deckId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task AddCardAsync(Card card, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeckExistsAsync(Guid deckId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
