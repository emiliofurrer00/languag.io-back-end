using System.Security.Claims;
using System.Text.Json;
using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Api.Contracts.Webhooks;
using Languag.io.Application.ActivityLogs;
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
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new DeckService(repository, activityLogRepository);
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
                        Type = CardTypes.MultiChoice,
                        FrontText = "hola",
                        BackText = "hello",
                        ExampleSentence = "Hola, que tal?",
                        Order = 3,
                        Choices =
                        [
                            new CardChoice { Text = "hello", IsCorrect = true, Order = 0 },
                            new CardChoice { Text = "goodbye", IsCorrect = false, Order = 1 }
                        ]
                    }
                ]),
            ownerId);

        Assert.Equal(deckId, repository.AddedDeck!.Id);
        Assert.Equal(ownerId, repository.AddedDeck.OwnerId);
        Assert.Single(repository.AddedDeck.Cards);
        Assert.Equal("Hola, que tal?", repository.AddedDeck.Cards[0].ExampleSentence);
        Assert.Equal(3, repository.AddedDeck.Cards[0].Order);
        Assert.Equal(repository.AddedDeck.Id, repository.AddedDeck.Cards[0].DeckId);
        Assert.Equal(CardTypes.MultiChoice, repository.AddedDeck.Cards[0].Type);
        Assert.Collection(
            repository.AddedDeck.Cards[0].Choices,
            choice =>
            {
                Assert.NotEqual(Guid.Empty, choice.Id);
                Assert.Equal(repository.AddedDeck.Cards[0].Id, choice.CardId);
                Assert.Equal("hello", choice.Text);
                Assert.True(choice.IsCorrect);
            },
            choice =>
            {
                Assert.Equal(repository.AddedDeck.Cards[0].Id, choice.CardId);
                Assert.Equal("goodbye", choice.Text);
                Assert.False(choice.IsCorrect);
            });
        Assert.Collection(
            activityLogRepository.AddedLogs,
            activity =>
            {
                Assert.Equal(ownerId, activity.UserId);
                Assert.Equal(deckId, activity.DeckId);
                Assert.Equal(ActivityType.DeckCreated, activity.Type);
            },
            activity =>
            {
                Assert.Equal(ownerId, activity.UserId);
                Assert.Equal(deckId, activity.DeckId);
                Assert.Equal(ActivityType.FirstDeckCreated, activity.Type);
            });
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateDeckAsync_SkipsFirstDeckActivityWhenUserAlreadyHasDecks()
    {
        var repository = new CapturingDeckRepository
        {
            UserHasDecksResult = true
        };
        var activityLogRepository = new CapturingActivityLogRepository();
        var service = new DeckService(repository, activityLogRepository);
        var ownerId = Guid.NewGuid();

        var deckId = await service.CreateDeckAsync(
            new CreateDeckCommand(
                "Spanish Basics",
                "Common starter words",
                "Spanish",
                "teal",
                DeckVisibility.Private,
                []),
            ownerId);

        var activity = Assert.Single(activityLogRepository.AddedLogs);
        Assert.Equal(ownerId, activity.UserId);
        Assert.Equal(deckId, activity.DeckId);
        Assert.Equal(ActivityType.DeckCreated, activity.Type);
    }

    [Fact]
    public async Task UpdateDeckAsync_PreservesExistingCardIdsAndRemovesOnlyOmittedCards()
    {
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var retainedCardId = Guid.NewGuid();
        var removedCardId = Guid.NewGuid();
        var retainedChoiceId = Guid.NewGuid();
        var removedChoiceId = Guid.NewGuid();
        var originalCreatedAt = DateTime.UtcNow.AddDays(-7);
        var repository = new CapturingDeckRepository
        {
            DeckForUpdate = new Deck
            {
                Id = deckId,
                OwnerId = ownerId,
                Title = "Spanish Basics",
                Description = "Old description",
                Category = "Spanish",
                Color = "teal",
                Visibility = DeckVisibility.Private,
                CreatedAtUtc = originalCreatedAt,
                UpdatedAtUtc = originalCreatedAt,
                Cards =
                [
                    new Card
                    {
                        Id = retainedCardId,
                        DeckId = deckId,
                        Type = CardTypes.MultiChoice,
                        FrontText = "hola",
                        BackText = "hello",
                        ExampleSentence = "Hola.",
                        Order = 0,
                        Choices =
                        [
                            new CardChoice
                            {
                                Id = retainedChoiceId,
                                CardId = retainedCardId,
                                Text = "hello",
                                IsCorrect = true,
                                Order = 0
                            },
                            new CardChoice
                            {
                                Id = removedChoiceId,
                                CardId = retainedCardId,
                                Text = "goodbye",
                                IsCorrect = false,
                                Order = 1
                            }
                        ],
                        CreatedAtUtc = originalCreatedAt,
                        UpdatedAtUtc = originalCreatedAt
                    },
                    new Card
                    {
                        Id = removedCardId,
                        DeckId = deckId,
                        FrontText = "adios",
                        BackText = "goodbye",
                        Order = 1,
                        CreatedAtUtc = originalCreatedAt,
                        UpdatedAtUtc = originalCreatedAt
                    }
                ]
            }
        };
        var service = new DeckService(repository, new CapturingActivityLogRepository());

        var result = await service.UpdateDeckAsync(
            new UpdateDeckCommand(
                deckId,
                "Spanish Basics Updated",
                "New description",
                "Language",
                "blue",
                DeckVisibility.Public,
                [
                    new Card
                    {
                        Id = retainedCardId,
                        Type = CardTypes.MultiChoice,
                        FrontText = "hola!",
                        BackText = "hello!",
                        ExampleSentence = "Hola a todos.",
                        Order = 2,
                        Choices =
                        [
                            new CardChoice
                            {
                                Id = retainedChoiceId,
                                Text = "hello!",
                                IsCorrect = true,
                                Order = 0
                            },
                            new CardChoice
                            {
                                Text = "thanks",
                                IsCorrect = false,
                                Order = 1
                            }
                        ]
                    },
                    new Card
                    {
                        FrontText = "gracias",
                        BackText = "thanks",
                        ExampleSentence = "Muchas gracias.",
                        Order = 1
                    }
                ]),
            ownerId);

        Assert.True(result);
        Assert.True(repository.SaveChangesCalled);
        var updatedCard = Assert.Single(repository.DeckForUpdate.Cards, card => card.Id == retainedCardId);
        Assert.Equal("hola!", updatedCard.FrontText);
        Assert.Equal("hello!", updatedCard.BackText);
        Assert.Equal("Hola a todos.", updatedCard.ExampleSentence);
        Assert.Equal(CardTypes.MultiChoice, updatedCard.Type);
        Assert.Equal(2, updatedCard.Order);
        Assert.Equal(originalCreatedAt, updatedCard.CreatedAtUtc);
        Assert.Collection(
            updatedCard.Choices.OrderBy(choice => choice.Order),
            choice =>
            {
                Assert.Equal(retainedChoiceId, choice.Id);
                Assert.Equal("hello!", choice.Text);
                Assert.True(choice.IsCorrect);
            },
            choice =>
            {
                Assert.NotEqual(Guid.Empty, choice.Id);
                Assert.Equal("thanks", choice.Text);
                Assert.False(choice.IsCorrect);
            });
        Assert.Equal(removedChoiceId, Assert.Single(repository.RemovedChoices).Id);

        var removedCard = Assert.Single(repository.RemovedCards);
        Assert.Equal(removedCardId, removedCard.Id);

        var addedCard = Assert.Single(repository.AddedCards);
        Assert.NotEqual(Guid.Empty, addedCard.Id);
        Assert.Equal(deckId, addedCard.DeckId);
        Assert.Equal("gracias", addedCard.FrontText);
        Assert.Equal("thanks", addedCard.BackText);
        Assert.Equal("Muchas gracias.", addedCard.ExampleSentence);
        Assert.Equal(1, addedCard.Order);
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
            "ada",
            "Ada Lovelace",
            "ada@example.com",
            true,
            25,
            "teal",
            null,
            null,
            "Linguist and builder",
            "I like language learning products.",
            true,
            DateTime.UtcNow);

        var service = new UserProfileService(new StubUserProfileRepository(expected));

        var profile = await service.GetByIdAsync(expected.Id);

        Assert.Equal(expected, profile);
    }

    [Fact]
    public async Task UserProfileService_ReturnsPublicProfileFromRepositoryByUsername()
    {
        var expected = new PublicUserProfileDto(
            Guid.NewGuid(),
            "ada",
            "Ada Lovelace",
            "teal",
            null,
            null,
            "Linguist and builder",
            "I like language learning products.",
            true,
            DateTime.UtcNow);

        var repository = new StubUserProfileRepository(
            new UserProfileDto(
                expected.Id,
                "kp_123",
                expected.Username,
                expected.Name,
                "ada@example.com",
                true,
                25,
                expected.AvatarColor,
                expected.ProfilePictureObjectKey,
                expected.ProfilePictureUrl,
                expected.ProfileDescription,
                expected.About,
                expected.IsPublicProfile,
                expected.CreatedAtUtc),
            expected);
        var service = new UserProfileService(repository);

        var profile = await service.GetPublicByUsernameAsync("  ada  ");

        Assert.Equal(expected, profile);
        Assert.Equal("ada", repository.LastPublicLookupUsername);
    }

    [Fact]
    public async Task UserProfileService_RejectsCompletedOnboardingWithoutUsername()
    {
        var service = new UserProfileService(new StubUserProfileRepository(new UserProfileDto(
            Guid.NewGuid(),
            "kp_123",
            null,
            "Ada Lovelace",
            "ada@example.com",
            false,
            0,
            "teal",
            null,
            null,
            "",
            "",
            false,
            DateTime.UtcNow)));

        var result = await service.UpdateAsync(new UpdateUserProfileCommand(
            Guid.NewGuid(),
            "   ",
            "Ada",
            true,
            25,
            "teal",
            "Bio",
            "About",
            false));

        Assert.Equal(UpdateUserProfileStatus.Invalid, result.Status);
        Assert.Null(result.Profile);
    }

    [Fact]
    public async Task UserProfileService_NormalizesUsernameBeforeCheckingAvailability()
    {
        var repository = new StubUserProfileRepository(new UserProfileDto(
            Guid.NewGuid(),
            "kp_123",
            "ada",
            "Ada Lovelace",
            "ada@example.com",
            true,
            25,
            "teal",
            null,
            null,
            "Bio",
            "About",
            false,
            DateTime.UtcNow));

        var service = new UserProfileService(repository);

        var isAvailable = await service.IsUsernameAvailableAsync("  ada  ", Guid.NewGuid());

        Assert.True(isAvailable);
        Assert.Equal("ada", repository.LastAvailabilityUsername);
    }

    private sealed class CapturingDeckRepository : IDeckRepository
    {
        public Deck? AddedDeck { get; private set; }
        public Deck? DeckForUpdate { get; init; }
        public List<Card> AddedCards { get; } = [];
        public List<Card> RemovedCards { get; } = [];
        public List<CardChoice> RemovedChoices { get; } = [];
        public bool SaveChangesCalled { get; private set; }
        public bool UserHasDecksResult { get; init; }

        public Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(DeckListQuery? query = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<DeckDto>> GetVisibleDecksAsync(Guid ownerId, DeckListQuery? query = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UserHasDecksAsync(Guid ownerId, CancellationToken ct = default)
        {
            return Task.FromResult(UserHasDecksResult);
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
            return Task.FromResult(
                DeckForUpdate is not null &&
                DeckForUpdate.Id == deckId &&
                DeckForUpdate.OwnerId == ownerId
                    ? DeckForUpdate
                    : null);
        }

        public void RemoveCards(IEnumerable<Card> cards)
        {
            RemovedCards.AddRange(cards);
        }

        public void RemoveCardChoices(IEnumerable<CardChoice> choices)
        {
            RemovedChoices.AddRange(choices);
        }

        public Task DeleteCardsByDeckIdAsync(Guid deckId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task AddCardAsync(Card card, CancellationToken ct = default)
        {
            AddedCards.Add(card);
            return Task.CompletedTask;
        }

        public Task<bool> DeckExistsAsync(Guid deckId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CapturingActivityLogRepository : IActivityLogRepository
    {
        public List<ActivityLog> AddedLogs { get; } = [];

        public Task AddAsync(ActivityLog activityLog, CancellationToken ct = default)
        {
            AddedLogs.Add(activityLog);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUserProfileRepository : IUserProfileRepository
    {
        private readonly UserProfileDto _profile;
        private readonly PublicUserProfileDto _publicProfile;
        public string? LastAvailabilityUsername { get; private set; }
        public string? LastPublicLookupUsername { get; private set; }
        public UpdateUserProfileCommand? LastUpdateCommand { get; private set; }

        public StubUserProfileRepository(UserProfileDto profile, PublicUserProfileDto? publicProfile = null)
        {
            _profile = profile;
            _publicProfile = publicProfile ?? new PublicUserProfileDto(
                profile.Id,
                profile.Username ?? "user",
                profile.Name,
                profile.AvatarColor,
                profile.ProfilePictureObjectKey,
                profile.ProfilePictureUrl,
                profile.ProfileDescription,
                profile.About,
                profile.IsPublicProfile,
                profile.CreatedAtUtc,
                profile.RecentActivity,
                profile.Stats);
        }

        public Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult<UserProfileDto?>(_profile with { Id = userId });
        }

        public Task<PublicUserProfileDto?> GetPublicByUsernameAsync(string username, CancellationToken ct = default)
        {
            LastPublicLookupUsername = username;
            return Task.FromResult<PublicUserProfileDto?>(_publicProfile with { Username = username });
        }

        public Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default)
        {
            LastAvailabilityUsername = username;
            return Task.FromResult(true);
        }

        public Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default)
        {
            LastUpdateCommand = command;
            return Task.FromResult(new UpdateUserProfileResult(
                UpdateUserProfileStatus.Updated,
                _profile with
                {
                    Id = command.UserId,
                    Username = command.Username,
                    Name = command.Name,
                    HasBeenOnboarded = command.HasBeenOnboarded,
                    DailyCardsGoal = command.DailyCardsGoal,
                    ProfileDescription = command.ProfileDescription,
                    About = command.About,
                    IsPublicProfile = command.IsPublicProfile
                }));
        }

        public Task<UpdateUserProfileResult> UpdateProfilePictureObjectKeyAsync(Guid userId, string objectKey, CancellationToken ct = default)
        {
            return Task.FromResult(new UpdateUserProfileResult(
                UpdateUserProfileStatus.Updated,
                _profile with
                {
                    Id = userId,
                    ProfilePictureObjectKey = objectKey,
                    ProfilePictureUrl = $"https://cdn.example.test/{objectKey}"
                },
                _profile.ProfilePictureObjectKey));
        }
    }
}
