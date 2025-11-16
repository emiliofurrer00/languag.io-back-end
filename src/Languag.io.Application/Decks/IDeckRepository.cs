using Languag.io.Domain.Entities;

namespace Languag.io.Application.Decks;

public interface IDeckRepository
{
    Task<IReadOnlyList<Deck>> GetPublicDecksAsync(CancellationToken ct = default);

    Task AddAsync(Deck deck, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
