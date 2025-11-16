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
            LanguageCode = command.LanguageCode,
            Visibility = command.Visibility,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _deckRepository.AddAsync(newDeck, ct);
        await _deckRepository.SaveChangesAsync(ct);

        return newDeck.Id;
    }
}


