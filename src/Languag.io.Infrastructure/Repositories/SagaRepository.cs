using Languag.io.Application.Sagas;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class SagaRepository : ISagaRepository
{
    private readonly AppDbContext _dbContext;

    public SagaRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SagaDto>> GetPublicSagasAsync(CancellationToken ct = default)
    {
        var sagas = await IncludeSagaGraph(_dbContext.Sagas.AsNoTracking(), currentUserId: null)
            .Where(saga => saga.Visibility == DeckVisibility.Public)
            .OrderByDescending(saga => saga.UpdatedAtUtc)
            .ToListAsync(ct);

        return sagas.Select(saga => MapSaga(saga, currentUserId: null)).ToList();
    }

    public async Task<IReadOnlyList<SagaDto>> GetVisibleSagasAsync(Guid userId, CancellationToken ct = default)
    {
        var sagas = await IncludeSagaGraph(_dbContext.Sagas.AsNoTracking(), userId)
            .Where(saga => saga.OwnerId == userId || saga.Visibility == DeckVisibility.Public)
            .OrderByDescending(saga => saga.UpdatedAtUtc)
            .ToListAsync(ct);

        return sagas.Select(saga => MapSaga(saga, userId)).ToList();
    }

    public async Task<SagaDto?> GetSagaByIdAsync(
        Guid sagaId,
        Guid? currentUserId,
        CancellationToken ct = default)
    {
        var query = IncludeSagaGraph(_dbContext.Sagas.AsNoTracking(), currentUserId)
            .Where(saga => saga.Id == sagaId)
            .Where(saga => saga.Visibility == DeckVisibility.Public ||
                (currentUserId.HasValue && saga.OwnerId == currentUserId.Value));

        var saga = await query.FirstOrDefaultAsync(ct);
        return saga is null ? null : MapSaga(saga, currentUserId);
    }

    public Task<Saga?> GetVisibleSagaForProgressAsync(
        Guid sagaId,
        Guid userId,
        CancellationToken ct = default)
    {
        return _dbContext.Sagas
            .AsNoTracking()
            .Include(saga => saga.Chapters)
                .ThenInclude(chapter => chapter.Lessons)
            .Where(saga => saga.Id == sagaId)
            .Where(saga => saga.OwnerId == userId || saga.Visibility == DeckVisibility.Public)
            .FirstOrDefaultAsync(ct);
    }

    public Task<SagaProgress?> GetProgressForUpdateAsync(
        Guid sagaId,
        Guid userId,
        CancellationToken ct = default)
    {
        return _dbContext.SagaProgresses
            .FirstOrDefaultAsync(progress => progress.SagaId == sagaId && progress.UserId == userId, ct);
    }

    public async Task<bool> CanAccessDecksAsync(
        IReadOnlyCollection<Guid> deckIds,
        Guid userId,
        CancellationToken ct = default)
    {
        var requestedDeckIds = deckIds.Distinct().ToArray();
        if (requestedDeckIds.Length == 0)
        {
            return false;
        }

        var visibleDeckCount = await _dbContext.Decks
            .AsNoTracking()
            .Where(deck => requestedDeckIds.Contains(deck.Id))
            .Where(deck => deck.OwnerId == userId || deck.Visibility == DeckVisibility.Public)
            .Select(deck => deck.Id)
            .Distinct()
            .CountAsync(ct);

        return visibleDeckCount == requestedDeckIds.Length;
    }

    public async Task<bool> AreDecksPublicAsync(
        IReadOnlyCollection<Guid> deckIds,
        CancellationToken ct = default)
    {
        var requestedDeckIds = deckIds.Distinct().ToArray();
        if (requestedDeckIds.Length == 0)
        {
            return false;
        }

        var publicDeckCount = await _dbContext.Decks
            .AsNoTracking()
            .Where(deck => requestedDeckIds.Contains(deck.Id))
            .Where(deck => deck.Visibility == DeckVisibility.Public)
            .Select(deck => deck.Id)
            .Distinct()
            .CountAsync(ct);

        return publicDeckCount == requestedDeckIds.Length;
    }

    public async Task AddAsync(Saga saga, CancellationToken ct = default)
    {
        await _dbContext.Sagas.AddAsync(saga, ct);
    }

    public async Task AddProgressAsync(SagaProgress progress, CancellationToken ct = default)
    {
        await _dbContext.SagaProgresses.AddAsync(progress, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    private static IQueryable<Saga> IncludeSagaGraph(IQueryable<Saga> query, Guid? currentUserId)
    {
        query = query
            .AsSplitQuery()
            .Include(saga => saga.User)
            .Include(saga => saga.Chapters)
                .ThenInclude(chapter => chapter.Lessons)
                    .ThenInclude(lesson => lesson.Deck)
                        .ThenInclude(deck => deck.Cards);

        if (currentUserId.HasValue)
        {
            query = query.Include(saga => saga.Progresses.Where(progress => progress.UserId == currentUserId.Value));
        }

        return query;
    }

    private static SagaDto MapSaga(Saga saga, Guid? currentUserId)
    {
        var orderedChapters = saga.Chapters
            .OrderBy(chapter => chapter.Order)
            .ToList();
        var orderedLessons = orderedChapters
            .SelectMany(chapter => chapter.Lessons.OrderBy(lesson => lesson.Order))
            .ToList();
        var progress = currentUserId.HasValue
            ? saga.Progresses.FirstOrDefault(progress => progress.UserId == currentUserId.Value)
            : null;

        return new SagaDto(
            saga.Id,
            saga.Title,
            saga.Category ?? string.Empty,
            saga.Description,
            saga.Visibility,
            saga.Color,
            orderedChapters.Select(MapChapter).ToList(),
            saga.User.Username ?? string.Empty,
            saga.User.Name ?? string.Empty,
            currentUserId.HasValue && saga.OwnerId == currentUserId.Value,
            MapProgress(progress, orderedLessons));
    }

    private static SagaChapterDto MapChapter(SagaChapter chapter)
    {
        return new SagaChapterDto(
            chapter.Id,
            chapter.Title,
            chapter.Description,
            chapter.Order,
            chapter.Lessons
                .OrderBy(lesson => lesson.Order)
                .Select(MapLesson)
                .ToList());
    }

    private static SagaLessonDto MapLesson(SagaLesson lesson)
    {
        return new SagaLessonDto(
            lesson.Id,
            lesson.DeckId,
            lesson.Deck.Title,
            lesson.Title,
            lesson.Description,
            lesson.Order,
            lesson.Deck.Cards.Count);
    }

    private static SagaProgressDto MapProgress(SagaProgress? progress, IReadOnlyList<SagaLesson> orderedLessons)
    {
        var highestIndex = progress?.HighestCompletedLessonId is Guid highestCompletedLessonId
            ? orderedLessons.ToList().FindIndex(lesson => lesson.Id == highestCompletedLessonId)
            : -1;
        var completedLessonCount = highestIndex < 0 ? 0 : highestIndex + 1;
        var currentLessonId = completedLessonCount < orderedLessons.Count
            ? orderedLessons[completedLessonCount].Id
            : (Guid?)null;
        var percentageComplete = orderedLessons.Count == 0
            ? 0m
            : decimal.Round(completedLessonCount * 100m / orderedLessons.Count, 2);

        return new SagaProgressDto(
            progress?.LastStudiedLessonId,
            progress?.HighestCompletedLessonId,
            currentLessonId,
            completedLessonCount,
            orderedLessons.Count,
            percentageComplete,
            progress?.StartedAtUtc,
            progress?.LastStudiedAtUtc,
            progress?.CompletedAtUtc);
    }
}
