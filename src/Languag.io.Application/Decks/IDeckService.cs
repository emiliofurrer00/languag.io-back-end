using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckService
{
    Task<IEnumerable<Deck>> GetPublicDecksAsync(CancellationToken ct = default);
    Task<Guid> CreateDeckAsync(CreateDeckCommand command, Guid ownerId, CancellationToken ct = default);
    Task<Deck?> GetDeckByIdAsync(Guid deckId, CancellationToken ct = default);
    Task<bool> UpdateDeckAsync(UpdateDeckCommand command, Guid ownerId, CancellationToken ct = default);
}
