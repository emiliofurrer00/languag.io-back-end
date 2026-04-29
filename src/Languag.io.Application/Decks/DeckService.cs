using Languag.io.Domain.Entities;
using Languag.io.Application.ActivityLogs;
using Languag.io.Api.Contracts.Decks;

namespace Languag.io.Application.Decks;

public class DeckService : IDeckService 
{
    private readonly IDeckRepository _deckRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    
    public DeckService(
        IDeckRepository deckRepository,
        IActivityLogRepository activityLogRepository)
    {
        _deckRepository = deckRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IEnumerable<DeckDto>> GetPublicDecksAsync(DeckListQuery? query = null, CancellationToken ct = default)
    {
        return await _deckRepository.GetPublicDecksAsync(query, ct);
    }

    public async Task<IEnumerable<DeckDto>> GetVisibleDecksAsync(Guid ownerId, DeckListQuery? query = null, CancellationToken ct = default)
    {
        return await _deckRepository.GetVisibleDecksAsync(ownerId, query, ct);
    }

    public async Task<Guid> CreateDeckAsync(CreateDeckCommand command, Guid ownerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var isFirstDeck = !await _deckRepository.UserHasDecksAsync(ownerId, ct);

        Deck newDeck = new Deck
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            OwnerId = ownerId,
            Description = command.Description,
            Category = command.Category,
            Color = command.Color,
            Visibility = command.Visibility,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        Card[] cards = command.Cards
            .Select(c => CreateCard(c, newDeck.Id, now))
            .ToArray();

        newDeck.Cards = cards.ToList();


        await _deckRepository.AddAsync(newDeck, ct);
        ActivityLog deckCreatedLog = CreateActivityLog(ownerId, newDeck, ActivityType.DeckCreated, now);
        await _activityLogRepository.AddAsync(deckCreatedLog, ct);

        if (isFirstDeck)
        {
            await _activityLogRepository.AddAsync(
                CreateActivityLog(ownerId, newDeck, ActivityType.FirstDeckCreated, now),
                ct);
        }

        await _deckRepository.SaveChangesAsync(ct);

        return newDeck.Id;
    }

    public async Task<DeckDto?> GetDeckByIdAsync(Guid deckId, Guid? ownerId, CancellationToken ct = default)
    {
        return await _deckRepository.GetDeckByIdAsync(deckId, ownerId, ct);
    }

    public async Task<bool> UpdateDeckAsync(UpdateDeckCommand command, Guid ownerId, CancellationToken ct = default)
    {
        var deck = await _deckRepository.GetDeckByIdForUpdateAsync(command.Id, ownerId, ct);
        if (deck is null) return false;

        var now = DateTime.UtcNow;

        deck.Title = command.Title;
        deck.Description = command.Description;
        deck.Category = command.Category;
        deck.Color = command.Color;
        deck.Visibility = command.Visibility;
        deck.UpdatedAtUtc = now;

        var existingCardsById = deck.Cards.ToDictionary(card => card.Id);
        var retainedCardIds = new HashSet<Guid>();

        foreach (var dto in command.Cards.OrderBy(c => c.Order))
        {
            if (dto.Id != Guid.Empty &&
                existingCardsById.TryGetValue(dto.Id, out var existingCard))
            {
                if (!retainedCardIds.Add(existingCard.Id))
                {
                    continue;
                }

                existingCard.FrontText = dto.FrontText;
                existingCard.BackText = dto.BackText;
                existingCard.ExampleSentence = dto.ExampleSentence;
                existingCard.Type = CardTypes.Normalize(dto.Type);
                existingCard.Order = dto.Order;
                existingCard.UpdatedAtUtc = now;
                SyncChoices(existingCard, dto.Choices);
                continue;
            }

            await _deckRepository.AddCardAsync(CreateCard(dto, deck.Id, now), ct);
        }

        var removedCards = deck.Cards
            .Where(card => !retainedCardIds.Contains(card.Id))
            .ToArray();

        _deckRepository.RemoveCards(removedCards);

        await _deckRepository.SaveChangesAsync(ct);
        return true;
    }

    private static Card CreateCard(Card source, Guid deckId, DateTime now)
    {
        var id = Guid.NewGuid();
        return new Card
        {
            Id = id,
            DeckId = deckId,
            Type = CardTypes.Normalize(source.Type),
            FrontText = source.FrontText,
            BackText = source.BackText,
            ExampleSentence = source.ExampleSentence,
            Order = source.Order,
            Choices = (source.Choices ?? [])
                .OrderBy(choice => choice.Order)
                .Select(choice => CreateChoice(choice, id))
                .ToList(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static CardChoice CreateChoice(CardChoice source, Guid cardId)
    {
        return new CardChoice
        {
            Id = Guid.NewGuid(),
            CardId = cardId,
            Text = source.Text,
            IsCorrect = source.IsCorrect,
            Order = source.Order
        };
    }

    private void SyncChoices(Card card, IReadOnlyCollection<CardChoice>? requestedChoices)
    {
        var existingChoicesById = card.Choices.ToDictionary(choice => choice.Id);
        var retainedChoiceIds = new HashSet<Guid>();

        foreach (var requestedChoice in (requestedChoices ?? []).OrderBy(choice => choice.Order))
        {
            if (requestedChoice.Id != Guid.Empty &&
                existingChoicesById.TryGetValue(requestedChoice.Id, out var existingChoice))
            {
                if (!retainedChoiceIds.Add(existingChoice.Id))
                {
                    continue;
                }

                existingChoice.Text = requestedChoice.Text;
                existingChoice.IsCorrect = requestedChoice.IsCorrect;
                existingChoice.Order = requestedChoice.Order;
                continue;
            }

            card.Choices.Add(CreateChoice(requestedChoice, card.Id));
        }

        var removedChoices = card.Choices
            .Where(choice => existingChoicesById.ContainsKey(choice.Id) && !retainedChoiceIds.Contains(choice.Id))
            .ToArray();

        foreach (var removedChoice in removedChoices)
        {
            card.Choices.Remove(removedChoice);
        }

        _deckRepository.RemoveCardChoices(removedChoices);
    }

    private static ActivityLog CreateActivityLog(
        Guid userId,
        Deck deck,
        ActivityType activityType,
        DateTime occurredAtUtc)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeckId = deck.Id,
            Deck = deck,
            Type = activityType,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = occurredAtUtc
        };
    }

}


