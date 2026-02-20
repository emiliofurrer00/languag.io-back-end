using Languag.io.Api.Contracts.Decks;
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
    public async Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .Where(d => d.Visibility == DeckVisibility.Public)
            .Include(d => d.Cards)
            .Select(d => new DeckDto(
                d.Id,
                d.Title,
                d.Category,
                d.Description,
                d.Visibility,
                d.Color,
                d.Cards
                    .OrderBy(c => c.Order)
                    .Select(c => new CardDto(c.Id, c.FrontText, c.BackText, c.Order))
                    .ToList()
            ))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Deck deck, CancellationToken ct = default)
    {
        await _dbContext.Decks.AddAsync(deck, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<DeckDto?> GetDeckByIdAsync(Guid deckId, CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .AsNoTracking()
            .Where(d => d.Id == deckId)
            .Include(d => d.Cards)
            .Select(d => new DeckDto(
                d.Id,
                d.Title,
                d.Category,
                d.Description,
                d.Visibility,
                d.Color,
                d.Cards
                    .OrderBy(c => c.Order)
                    .Select(c => new CardDto(c.Id, c.FrontText, c.BackText, c.Order))
                    .ToList()
             ))
            .FirstOrDefaultAsync();
    }

    public async Task<Deck?> GetDeckByIdForUpdateAsync(Guid deckId, CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == deckId, ct);
    }
    public async Task UpdateAsync(Deck deck, CancellationToken ct = default)
    {
        _dbContext.Decks.Update(deck);
        await _dbContext.SaveChangesAsync(ct);
    }
}