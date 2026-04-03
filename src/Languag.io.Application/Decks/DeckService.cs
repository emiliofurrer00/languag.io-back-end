using Languag.io.Domain.Entities;
using Languag.io.Application.Decks;
using Languag.io.Api.Contracts.Decks;

namespace Languag.io.Application.Decks;

public class DeckService : IDeckService 
{
    private readonly IDeckRepository _deckRepository;
    
    public DeckService(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<IEnumerable<DeckDto>> GetPublicDecksAsync(CancellationToken ct = default)
    {
        return await _deckRepository.GetPublicDecksAsync(ct);
    }

    public async Task<Guid> CreateDeckAsync(CreateDeckCommand command, Guid ownerId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

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

        Card[] cards = command.Cards.Select(c => new Card
        {
            Id = Guid.NewGuid(),
            DeckId = newDeck.Id,
            FrontText = c.FrontText,
            BackText = c.BackText,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }).ToArray();

        newDeck.Cards = cards.ToList();


        await _deckRepository.AddAsync(newDeck, ct);
        await _deckRepository.SaveChangesAsync(ct);

        return newDeck.Id;
    }

    public async Task<DeckDto?> GetDeckByIdAsync(Guid deckId, CancellationToken ct = default)
    {
        return await _deckRepository.GetDeckByIdAsync(deckId, ct);
    }

    public async Task<bool> UpdateDeckAsync(UpdateDeckCommand command, Guid ownerId, CancellationToken ct = default)
    {
        var deck = await _deckRepository.GetDeckByIdForUpdateAsync(command.Id, ct);
        if (deck is null) return false;

        var now = DateTime.UtcNow;

        deck.Title = command.Title;
        deck.Description = command.Description;
        deck.Category = command.Category;
        deck.Color = command.Color;
        deck.Visibility = command.Visibility;
        deck.UpdatedAtUtc = now;

        // Set-based delete (no tracked cards exist now)
        await _deckRepository.DeleteCardsByDeckIdAsync(deck.Id, ct);

        // Insert new cards (ignore incoming ids)
        foreach (var dto in command.Cards.OrderBy(c => c.Order))
        {
            await _deckRepository.AddCardAsync(new Card
            {
                Id = Guid.NewGuid(),
                DeckId = deck.Id,
                FrontText = dto.FrontText,
                BackText = dto.BackText,
                Order = dto.Order,
                ExampleSentence = dto.ExampleSentence,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, ct);
        }

        await _deckRepository.SaveChangesAsync(ct);
        return true;
    }

}


