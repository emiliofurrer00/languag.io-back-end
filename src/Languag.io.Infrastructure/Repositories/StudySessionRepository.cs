using Languag.io.Application.StudySessions;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class StudySessionRepository : IStudySessionRepository
{
    private readonly AppDbContext _dbContext;

    public StudySessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CanAccessDeckAsync(Guid deckId, Guid userId, CancellationToken ct = default)
    {
        return _dbContext.Decks
            .AsNoTracking()
            .AnyAsync(
                deck => deck.Id == deckId
                    && (deck.OwnerId == userId || deck.Visibility == DeckVisibility.Public),
                ct);
    }

    public async Task<bool> DeckContainsCardsAsync(
        Guid deckId,
        IReadOnlyCollection<Guid> cardIds,
        CancellationToken ct = default)
    {
        var distinctCardIds = cardIds.Distinct().ToList();
        if (distinctCardIds.Count == 0)
        {
            return false;
        }

        var matchingCardCount = await _dbContext.Cards
            .AsNoTracking()
            .Where(card => card.DeckId == deckId && distinctCardIds.Contains(card.Id))
            .Select(card => card.Id)
            .Distinct()
            .CountAsync(ct);

        return matchingCardCount == distinctCardIds.Count;
    }

    public Task<bool> UserHasStudySessionsAsync(Guid userId, CancellationToken ct = default)
    {
        return _dbContext.StudySessions
            .AsNoTracking()
            .AnyAsync(studySession => studySession.UserId == userId, ct);
    }

    public async Task AddAsync(StudySession studySession, CancellationToken ct = default)
    {
        await _dbContext.StudySessions.AddAsync(studySession, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }
}
