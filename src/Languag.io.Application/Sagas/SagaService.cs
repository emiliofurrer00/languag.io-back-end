using Languag.io.Domain.Entities;

namespace Languag.io.Application.Sagas;

public sealed class SagaService : ISagaService
{
    private readonly ISagaRepository _sagaRepository;

    public SagaService(ISagaRepository sagaRepository)
    {
        _sagaRepository = sagaRepository;
    }

    public Task<IReadOnlyList<SagaDto>> GetPublicSagasAsync(CancellationToken ct = default)
    {
        return _sagaRepository.GetPublicSagasAsync(ct);
    }

    public Task<IReadOnlyList<SagaDto>> GetVisibleSagasAsync(Guid userId, CancellationToken ct = default)
    {
        return _sagaRepository.GetVisibleSagasAsync(userId, ct);
    }

    public Task<SagaDto?> GetSagaByIdAsync(Guid sagaId, Guid? currentUserId, CancellationToken ct = default)
    {
        return _sagaRepository.GetSagaByIdAsync(sagaId, currentUserId, ct);
    }

    public async Task<CreateSagaResult> CreateSagaAsync(
        CreateSagaCommand command,
        Guid ownerId,
        CancellationToken ct = default)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new CreateSagaResult(CreateSagaStatus.Invalid, Error: validationError);
        }

        var deckIds = command.Chapters
            .SelectMany(chapter => chapter.Lessons)
            .Select(lesson => lesson.DeckId)
            .Distinct()
            .ToArray();

        if (!await _sagaRepository.CanAccessDecksAsync(deckIds, ownerId, ct))
        {
            return new CreateSagaResult(
                CreateSagaStatus.DeckNotFound,
                Error: "One or more lessons reference decks that are not visible to the current user.");
        }

        if (command.Visibility == DeckVisibility.Public &&
            !await _sagaRepository.AreDecksPublicAsync(deckIds, ct))
        {
            return new CreateSagaResult(
                CreateSagaStatus.Invalid,
                Error: "Public sagas can only include public decks.");
        }

        var now = DateTime.UtcNow;
        var saga = new Saga
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = command.Title.Trim(),
            Description = TrimToNull(command.Description),
            Category = command.Category?.Trim() ?? string.Empty,
            Color = TrimToNull(command.Color),
            Visibility = command.Visibility,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Chapters = command.Chapters
                .Select((chapter, chapterIndex) => CreateChapter(chapter, chapterIndex))
                .ToList()
        };

        foreach (var chapter in saga.Chapters)
        {
            chapter.SagaId = saga.Id;
        }

        await _sagaRepository.AddAsync(saga, ct);
        await _sagaRepository.SaveChangesAsync(ct);

        return new CreateSagaResult(CreateSagaStatus.Created, SagaId: saga.Id);
    }

    public async Task<CompleteSagaLessonResult> CompleteLessonAsync(
        Guid sagaId,
        Guid lessonId,
        Guid userId,
        CancellationToken ct = default)
    {
        var saga = await _sagaRepository.GetVisibleSagaForProgressAsync(sagaId, userId, ct);
        if (saga is null)
        {
            return new CompleteSagaLessonResult(CompleteSagaLessonStatus.SagaNotFound);
        }

        var orderedLessons = GetOrderedLessons(saga);
        if (orderedLessons.Count == 0)
        {
            return new CompleteSagaLessonResult(
                CompleteSagaLessonStatus.Invalid,
                Error: "Saga does not contain any lessons.");
        }

        var lessonIndex = orderedLessons.FindIndex(lesson => lesson.Id == lessonId);
        if (lessonIndex < 0)
        {
            return new CompleteSagaLessonResult(CompleteSagaLessonStatus.LessonNotFound);
        }

        var now = DateTime.UtcNow;
        var progress = await _sagaRepository.GetProgressForUpdateAsync(sagaId, userId, ct);
        if (progress is null)
        {
            progress = new SagaProgress
            {
                SagaId = sagaId,
                UserId = userId,
                StartedAtUtc = now
            };
            await _sagaRepository.AddProgressAsync(progress, ct);
        }

        var highestIndex = progress.HighestCompletedLessonId.HasValue
            ? orderedLessons.FindIndex(lesson => lesson.Id == progress.HighestCompletedLessonId.Value)
            : -1;

        progress.LastStudiedLessonId = lessonId;
        progress.LastStudiedAtUtc = now;

        if (lessonIndex > highestIndex)
        {
            progress.HighestCompletedLessonId = lessonId;
            highestIndex = lessonIndex;
        }

        if (highestIndex == orderedLessons.Count - 1)
        {
            progress.CompletedAtUtc ??= now;
        }
        else
        {
            progress.CompletedAtUtc = null;
        }

        await _sagaRepository.SaveChangesAsync(ct);

        return new CompleteSagaLessonResult(
            CompleteSagaLessonStatus.Completed,
            Progress: MapProgress(progress, orderedLessons));
    }

    private static SagaChapter CreateChapter(CreateSagaChapterCommand source, int index)
    {
        var chapter = new SagaChapter
        {
            Id = Guid.NewGuid(),
            Title = source.Title.Trim(),
            Description = TrimToNull(source.Description),
            Order = NormalizeOrder(source.Order, index),
            Lessons = source.Lessons
                .Select((lesson, lessonIndex) => CreateLesson(lesson, lessonIndex))
                .ToList()
        };

        foreach (var lesson in chapter.Lessons)
        {
            lesson.SagaChapterId = chapter.Id;
        }

        return chapter;
    }

    private static SagaLesson CreateLesson(CreateSagaLessonCommand source, int index)
    {
        return new SagaLesson
        {
            Id = Guid.NewGuid(),
            DeckId = source.DeckId,
            Title = TrimToNull(source.Title),
            Description = TrimToNull(source.Description),
            Order = NormalizeOrder(source.Order, index)
        };
    }

    private static string? Validate(CreateSagaCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return "Title is required.";
        }

        if (command.Title.Length > 200)
        {
            return "Title cannot exceed 200 characters.";
        }

        if (command.Description?.Length > 1000)
        {
            return "Description cannot exceed 1000 characters.";
        }

        if (command.Category?.Length > 80)
        {
            return "Category cannot exceed 80 characters.";
        }

        if (command.Color?.Length > 20)
        {
            return "Color cannot exceed 20 characters.";
        }

        if (command.Chapters.Count == 0)
        {
            return "A saga must include at least one chapter.";
        }

        if (command.Chapters.Count > 100)
        {
            return "A saga cannot include more than 100 chapters.";
        }

        foreach (var chapter in command.Chapters)
        {
            if (string.IsNullOrWhiteSpace(chapter.Title))
            {
                return "Every chapter must include a title.";
            }

            if (chapter.Title.Length > 200)
            {
                return "Chapter titles cannot exceed 200 characters.";
            }

            if (chapter.Description?.Length > 1000)
            {
                return "Chapter descriptions cannot exceed 1000 characters.";
            }

            if (chapter.Lessons.Count == 0)
            {
                return "Every chapter must include at least one lesson.";
            }

            if (chapter.Lessons.Count > 200)
            {
                return "A chapter cannot include more than 200 lessons.";
            }

            foreach (var lesson in chapter.Lessons)
            {
                if (lesson.DeckId == Guid.Empty)
                {
                    return "Every lesson must reference a deck.";
                }

                if (lesson.Title?.Length > 200)
                {
                    return "Lesson titles cannot exceed 200 characters.";
                }

                if (lesson.Description?.Length > 1000)
                {
                    return "Lesson descriptions cannot exceed 1000 characters.";
                }
            }
        }

        return null;
    }

    private static List<SagaLesson> GetOrderedLessons(Saga saga)
    {
        return saga.Chapters
            .OrderBy(chapter => chapter.Order)
            .SelectMany(chapter => chapter.Lessons.OrderBy(lesson => lesson.Order))
            .ToList();
    }

    private static SagaProgressDto MapProgress(SagaProgress progress, IReadOnlyList<SagaLesson> orderedLessons)
    {
        var highestIndex = progress.HighestCompletedLessonId.HasValue
            ? orderedLessons.ToList().FindIndex(lesson => lesson.Id == progress.HighestCompletedLessonId.Value)
            : -1;
        var completedLessonCount = highestIndex < 0 ? 0 : highestIndex + 1;
        var currentLessonId = completedLessonCount < orderedLessons.Count
            ? orderedLessons[completedLessonCount].Id
            : (Guid?)null;
        var percentageComplete = orderedLessons.Count == 0
            ? 0m
            : decimal.Round(completedLessonCount * 100m / orderedLessons.Count, 2);

        return new SagaProgressDto(
            progress.LastStudiedLessonId,
            progress.HighestCompletedLessonId,
            currentLessonId,
            completedLessonCount,
            orderedLessons.Count,
            percentageComplete,
            progress.StartedAtUtc,
            progress.LastStudiedAtUtc,
            progress.CompletedAtUtc);
    }

    private static int NormalizeOrder(int requestedOrder, int fallbackIndex)
    {
        return requestedOrder > 0 ? requestedOrder : fallbackIndex;
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
