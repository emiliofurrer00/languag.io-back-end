using Languag.io.Api.Contracts.Decks;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckRepository
{
    Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(CancellationToken ct = default);

    Task AddAsync(Deck deck, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<DeckDto> GetDeckByIdAsync(Guid deckId, CancellationToken ct = default);
    Task<Deck?> GetDeckByIdForUpdateAsync(Guid deckId, CancellationToken ct = default); 
    Task UpdateAsync(Deck deck, CancellationToken ct = default);
}
