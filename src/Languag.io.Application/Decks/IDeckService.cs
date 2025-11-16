using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckService
{
    Task<IEnumerable<Deck>> GetPublicDecksAsync(CancellationToken ct = default);
}
