using Languag.io.Domain.Entities;

namespace Languag.io.Application.StudySessions;

public interface IStudySessionRepository
{
    Task<bool> CanAccessDeckAsync(Guid deckId, Guid userId, CancellationToken ct = default);
    Task<bool> DeckContainsCardsAsync(Guid deckId, IReadOnlyCollection<Guid> cardIds, CancellationToken ct = default);
    Task<bool> UserHasStudySessionsAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(StudySession studySession, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
