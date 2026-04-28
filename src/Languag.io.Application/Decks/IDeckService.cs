using Languag.io.Api.Contracts.Decks;
using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckService
{
    Task<IEnumerable<DeckDto>> GetPublicDecksAsync(DeckListQuery? query = null, CancellationToken ct = default);
    Task<IEnumerable<DeckDto>> GetVisibleDecksAsync(Guid ownerId, DeckListQuery? query = null, CancellationToken ct = default);
    Task<Guid> CreateDeckAsync(CreateDeckCommand command, Guid ownerId, CancellationToken ct = default);
    Task<DeckDto?> GetDeckByIdAsync(Guid deckId, Guid? ownerId, CancellationToken ct = default);
    Task<bool> UpdateDeckAsync(UpdateDeckCommand command, Guid ownerId, CancellationToken ct = default);
}
