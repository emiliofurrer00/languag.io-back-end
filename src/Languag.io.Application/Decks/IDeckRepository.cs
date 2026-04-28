using Languag.io.Api.Contracts.Decks;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckRepository
{
    Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(DeckListQuery? query = null, CancellationToken ct = default);
    Task<IReadOnlyList<DeckDto>> GetVisibleDecksAsync(Guid ownerId, DeckListQuery? query = null, CancellationToken ct = default);
    Task<bool> UserHasDecksAsync(Guid ownerId, CancellationToken ct = default);

    Task AddAsync(Deck deck, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<DeckDto?> GetDeckByIdAsync(Guid deckId, Guid? ownerId, CancellationToken ct = default);
    Task<Deck?> GetDeckByIdForUpdateAsync(Guid deckId, Guid ownerId, CancellationToken ct = default);
    void RemoveCards(IEnumerable<Card> cards);
    Task DeleteCardsByDeckIdAsync(Guid deckId, CancellationToken ct = default);
    Task AddCardAsync(Card card, CancellationToken ct = default);
    Task<bool> DeckExistsAsync(Guid deckId, CancellationToken ct = default);
}
