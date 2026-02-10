using Languag.io.Domain.Entities;
using Languag.io.Application.Decks;

namespace Languag.io.Application.Decks;

public class DeckService : IDeckService 
{
    private readonly IDeckRepository _deckRepository;
    
    public DeckService(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<IEnumerable<Deck>> GetPublicDecksAsync(CancellationToken ct = default)
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

        await _deckRepository.AddAsync(newDeck, ct);
        await _deckRepository.SaveChangesAsync(ct);

        return newDeck.Id;
    }

    public async Task<Deck?> GetDeckByIdAsync(Guid deckId, CancellationToken ct = default)
    {
        return await _deckRepository.GetDeckByIdAsync(deckId, ct);
    }

    public async Task<bool> UpdateDeckAsync(UpdateDeckCommand command, Guid ownerId, CancellationToken ct = default)
    {
        var deck = await _deckRepository.GetDeckByIdAsync(command.Id, ct);
        if (deck == null || deck.OwnerId != ownerId)
        {
            return false; // Deck not found or user is not the owner
        }
        deck.Title = command.Title;
        deck.Description = command.Description;
        deck.Category = command.Category;
        deck.Color = command.Color;
        deck.Visibility = command.Visibility;
        deck.UpdatedAtUtc = DateTime.UtcNow;
        await _deckRepository.UpdateAsync(deck);
        await _deckRepository.SaveChangesAsync(ct);
        return true;
    }
}


