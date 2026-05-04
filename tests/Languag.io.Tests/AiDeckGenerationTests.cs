using Languag.io.Application.AiDeckGeneration;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.AiDeckGeneration;
using Languag.io.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Languag.io.Tests;

public class AiDeckGenerationTests
{
    [Fact]
    public async Task CreateDeckGenerationJobAsync_CreatesPendingJobForUser()
    {
        var repository = new CapturingAiDeckGenerationJobRepository();
        var service = new AiDeckGenerationService(repository);
        var userId = Guid.NewGuid();

        var jobId = await service.CreateDeckGenerationJobAsync(
            new CreateAiDeckGenerationJobCommand(
                "  restaurant phrases  ",
                " French ",
                " English ",
                "Beginner",
                10,
                true,
                3),
            userId);

        Assert.Equal(jobId, repository.Job!.Id);
        Assert.Equal(userId, repository.Job.UserId);
        Assert.Equal("restaurant phrases", repository.Job.Prompt);
        Assert.Equal("French", repository.Job.TargetLanguage);
        Assert.True(repository.Job.IncludeAudio);
        Assert.Equal(3, repository.Job.RequestedMultiChoiceCount);
        Assert.Equal(AiDeckAudioStatus.Pending, repository.Job.AudioStatus);
        Assert.Equal(AiDeckGenerationStatus.Pending, repository.Job.Status);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateDeckGenerationJobAsync_RejectsOutOfRangeCardCounts()
    {
        var service = new AiDeckGenerationService(new CapturingAiDeckGenerationJobRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateDeckGenerationJobAsync(
                new CreateAiDeckGenerationJobCommand(
                    "too many",
                    "French",
                    "English",
                    "Beginner",
                    25,
                    false),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateDeckGenerationJobAsync_RejectsMultiChoiceCountsAboveTotalCards()
    {
        var service = new AiDeckGenerationService(new CapturingAiDeckGenerationJobRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateDeckGenerationJobAsync(
                new CreateAiDeckGenerationJobCommand(
                    "too many choices",
                    "French",
                    "English",
                    "Beginner",
                    5,
                    false,
                    6),
                Guid.NewGuid()));
    }

    [Fact]
    public async Task ProcessNextPendingJobAsync_CreatesDeckAndMarksJobCompleted()
    {
        await using var context = await CreateContextAsync();
        var user = CreateUser();
        var job = new AiDeckGenerationJob
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Prompt = "coffee shop phrases",
            TargetLanguage = "Spanish",
            NativeLanguage = "English",
            Difficulty = "Beginner",
            RequestedCardCount = 5,
            RequestedMultiChoiceCount = 2,
            Status = AiDeckGenerationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.AiDeckGenerationJobs.Add(job);
        await context.SaveChangesAsync();

        var processor = new AiDeckGenerationProcessor(
            context,
            new StubAiDeckGenerator(),
            NullLogger<AiDeckGenerationProcessor>.Instance);

        var processed = await processor.ProcessNextPendingJobAsync();

        Assert.True(processed);

        var completedJob = await context.AiDeckGenerationJobs.SingleAsync();
        Assert.Equal(AiDeckGenerationStatus.Completed, completedJob.Status);
        Assert.NotNull(completedJob.CreatedDeckId);

        var deck = await context.Decks
            .Include(candidate => candidate.Cards)
                .ThenInclude(card => card.Choices)
            .SingleAsync(candidate => candidate.Id == completedJob.CreatedDeckId);

        Assert.Equal(user.Id, deck.OwnerId);
        Assert.Equal(DeckVisibility.Private, deck.Visibility);
        Assert.Equal("Spanish Cafe Basics", deck.Title);
        Assert.Equal(5, deck.Cards.Count);
        Assert.Equal(2, deck.Cards.Count(card => card.Type == CardTypes.MultiChoice));
        Assert.Equal(3, deck.Cards.Count(card => card.Type == CardTypes.Flashcard));
        Assert.All(
            deck.Cards.Where(card => card.Type == CardTypes.MultiChoice),
            card =>
            {
                Assert.Equal(4, card.Choices.Count);
                Assert.Single(card.Choices, choice => choice.IsCorrect);
            });
    }

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            ExternalId = $"kp_{Guid.NewGuid():N}",
            Username = "ai-learner",
            Email = "ai@example.com",
            Name = "AI Learner",
            AvatarColor = "teal",
            ProfileDescription = string.Empty,
            About = string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private sealed class StubAiDeckGenerator : IAiDeckGenerator
    {
        public Task<GeneratedDeckDto> GenerateDeckAsync(
            AiDeckGenerationJob job,
            CancellationToken ct = default)
        {
            return Task.FromResult(new GeneratedDeckDto
            {
                Title = "Spanish Cafe Basics",
                Description = "Useful cafe phrases.",
                Category = "Languages",
                Cards = Enumerable.Range(1, job.RequestedCardCount)
                    .Select(index =>
                    {
                        var isMultiChoice = index <= job.RequestedMultiChoiceCount;

                        return new GeneratedCardDto
                        {
                            Type = isMultiChoice ? CardTypes.MultiChoice : CardTypes.Flashcard,
                            FrontText = isMultiChoice ? $"What does frase {index} mean?" : $"frase {index}",
                            BackText = $"phrase {index}",
                            TtsText = $"frase {index}",
                            ExampleSentence = $"Example {index}.",
                            Choices = isMultiChoice
                                ? CreateChoices(index)
                                : []
                        };
                    })
                    .ToList()
            });
        }

        private static List<GeneratedCardChoiceDto> CreateChoices(int index)
        {
            return
            [
                new() { Text = $"phrase {index}", IsCorrect = true },
                new() { Text = $"distractor {index}.1", IsCorrect = false },
                new() { Text = $"distractor {index}.2", IsCorrect = false },
                new() { Text = $"distractor {index}.3", IsCorrect = false }
            ];
        }
    }

    private sealed class CapturingAiDeckGenerationJobRepository : IAiDeckGenerationJobRepository
    {
        public AiDeckGenerationJob? Job { get; private set; }
        public bool SaveChangesCalled { get; private set; }

        public Task AddAsync(AiDeckGenerationJob job, CancellationToken ct = default)
        {
            Job = job;
            return Task.CompletedTask;
        }

        public Task<AiDeckGenerationJob?> GetForUserAsync(
            Guid jobId,
            Guid userId,
            CancellationToken ct = default)
        {
            return Task.FromResult(
                Job is not null && Job.Id == jobId && Job.UserId == userId
                    ? Job
                    : null);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
