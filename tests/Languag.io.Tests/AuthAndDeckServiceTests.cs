using System.Security.Claims;
using System.Text.Json;
using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Api.Contracts.Webhooks;
using Languag.io.Application.Decks;
using Languag.io.Application.Users;
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

    [Fact]
    public void WebhookEnvelope_MapsKindeDocumentedUserPayloadShape()
    {
        const string json = """
            {
              "data": {
                "user": {
                  "email": "user@example.com",
                  "first_name": "Test",
                  "id": "kp_1234567890",
                  "is_password_reset_requested": false,
                  "is_suspended": false,
                  "last_name": "",
                  "organizations": [
                    {
                      "code": "org_1234567890",
                      "permissions": null,
                      "roles": null
                    }
                  ],
                  "phone": null,
                  "username": null
                }
              },
              "event_id": "event_1234567890",
              "event_timestamp": "2026-02-03T12:00:00.000Z",
              "source": "admin",
              "timestamp": "2026-02-03T12:00:00.000Z",
              "type": "user.created"
            }
            """;

        var payload = JsonSerializer.Deserialize<WebhookEnvelope>(json);

        Assert.NotNull(payload);
        Assert.Equal("user.created", payload!.Type);
        Assert.Equal("event_1234567890", payload.EventId);
        Assert.Equal("kp_1234567890", payload.Data?.User?.Id);
        Assert.Equal("user@example.com", payload.Data?.User?.Email);
        Assert.Single(payload.Data?.User?.Organizations ?? []);
    }

    [Fact]
    public async Task UserProfileService_ReturnsProfileFromRepository()
    {
        var expected = new UserProfileDto(
            Guid.NewGuid(),
            "kp_123",
            "Ada Lovelace",
            "ada@example.com",
            "Linguist and builder",
            "I like language learning products.",
            true);

        var service = new UserProfileService(new StubUserProfileRepository(expected));

        var profile = await service.GetByIdAsync(expected.Id);

        Assert.Equal(expected, profile);
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

    private sealed class StubUserProfileRepository : IUserProfileRepository
    {
        private readonly UserProfileDto _profile;

        public StubUserProfileRepository(UserProfileDto profile)
        {
            _profile = profile;
        }

        public Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult<UserProfileDto?>(_profile with { Id = userId });
        }
    }
}
