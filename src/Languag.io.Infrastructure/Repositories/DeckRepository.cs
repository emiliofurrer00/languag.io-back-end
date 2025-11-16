using Languag.io.Application.Decks;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public class DeckRepository : IDeckRepository
{
    private readonly AppDbContext _dbContext;
    public DeckRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<IReadOnlyList<Deck>> GetPublicDecksAsync(CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .Where(d => d.Visibility == DeckVisibility.Public)
            .Include(d => d.Cards)
            .OrderByDescending(d => d.CreatedAtUtc)
            .ToListAsync(ct);
    }
}