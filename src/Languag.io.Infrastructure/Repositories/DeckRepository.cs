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
    public async Task<IReadOnlyList<DeckDto>> GetPublicDecksAsync(DeckListQuery? query = null, CancellationToken ct = default)
    {
        var decksQuery = _dbContext.Decks
            .AsNoTracking()
            .Where(d => d.Visibility == DeckVisibility.Public)
            .Include(d => d.User);

        return await ApplyListFilters(decksQuery, query)
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Select(d => MapToDeckDto(d))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DeckDto>> GetVisibleDecksAsync(Guid ownerId, DeckListQuery? query = null, CancellationToken ct = default)
    {
        var decksQuery = _dbContext.Decks
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.OwnerId == ownerId || d.Visibility == DeckVisibility.Public);

        return await ApplyListFilters(decksQuery, query)
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Select(d => MapToDeckDto(d))
            .ToListAsync(ct);
    }

    public Task<bool> UserHasDecksAsync(Guid ownerId, CancellationToken ct = default)
    {
        return _dbContext.Decks
            .AsNoTracking()
            .AnyAsync(deck => deck.OwnerId == ownerId, ct);
    }

    public async Task AddAsync(Deck deck, CancellationToken ct = default)
    {
        await _dbContext.Decks.AddAsync(deck, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var types = ex.Entries.Select(e => e.Entity.GetType().Name).ToArray();
            throw new Exception("Concurrency conflict on: " + string.Join(", ", types), ex);
        }
    }

    public async Task<DeckDto?> GetDeckByIdAsync(Guid deckId, Guid? ownerId, CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => d.Id == deckId)
            .Where(d => d.Visibility == DeckVisibility.Public || (ownerId.HasValue && d.OwnerId == ownerId.Value))
            .Select(d => new DeckDto(
                d.Id,
                d.Title,
                d.Category ?? string.Empty,
                d.Description,
                d.Visibility,
                d.Color,
                d.Cards
                    .OrderBy(c => c.Order)
                    .Select(c => new CardDto(c.Id, c.FrontText, c.BackText, c.Order))
                    .ToList(),
                d.User != null ? d.User.Username ?? "" : ""
             ))
            .FirstOrDefaultAsync();
    }

    public async Task<Deck?> GetDeckByIdForUpdateAsync(Guid deckId, Guid ownerId, CancellationToken ct = default)
    {
        return await _dbContext.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == deckId && d.OwnerId == ownerId, ct);
    }

    public void RemoveCards(IEnumerable<Card> cards)
    {
        _dbContext.Cards.RemoveRange(cards);
    }

    public Task DeleteCardsByDeckIdAsync(Guid deckId, CancellationToken ct)
    {
        return _dbContext.Cards
            .Where(c => c.DeckId == deckId)
            .ExecuteDeleteAsync(ct);
    }

    public Task<bool> DeckExistsAsync(Guid deckId, CancellationToken ct) =>
    _dbContext.Decks.AnyAsync(d => d.Id == deckId, ct);

    public async Task AddCardAsync(Card card, CancellationToken ct) =>
        await _dbContext.Cards.AddAsync(card, ct);

    private static IQueryable<Deck> ApplyListFilters(IQueryable<Deck> query, DeckListQuery? filters)
    {
        var ownerUsername = filters?.NormalizedOwnerUsername;
        if (!string.IsNullOrWhiteSpace(ownerUsername))
        {
            var normalizedOwnerUsername = ownerUsername.ToLower();
            query = query.Where(d =>
                d.User != null &&
                d.User.Username != null &&
                d.User.Username.ToLower() == normalizedOwnerUsername);
        }

        var searchQuery = filters?.NormalizedSearchQuery;
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var normalizedSearchQuery = searchQuery.ToLower();
            query = query.Where(d =>
                d.Title.ToLower().Contains(normalizedSearchQuery) ||
                (d.Description != null && d.Description.ToLower().Contains(normalizedSearchQuery)) ||
                (d.Category != null && d.Category.ToLower().Contains(normalizedSearchQuery)) ||
                (d.User != null && d.User.Username != null && d.User.Username.ToLower().Contains(normalizedSearchQuery)) ||
                (d.User != null && d.User.Name != null && d.User.Name.ToLower().Contains(normalizedSearchQuery)));
        }

        return query;
    }

    private static DeckDto MapToDeckDto(Deck deck)
    {
        return new DeckDto(
            deck.Id,
            deck.Title,
            deck.Category ?? string.Empty,
            deck.Description,
            deck.Visibility,
            deck.Color,
            deck.Cards
                .OrderBy(c => c.Order)
                .Select(c => new CardDto(c.Id, c.FrontText, c.BackText, c.Order))
                .ToList(),
            deck.User != null ? deck.User.Username ?? "" : ""
        );
    }
}
