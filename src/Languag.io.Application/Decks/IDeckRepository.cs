using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckRepository
{
    Task<IReadOnlyList<Deck>> GetPublicDecksAsync(CancellationToken ct = default);
}
