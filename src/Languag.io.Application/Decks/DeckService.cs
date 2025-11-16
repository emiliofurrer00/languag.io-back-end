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
}


