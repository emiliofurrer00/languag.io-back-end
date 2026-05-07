using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiSagaGeneration;

public class AiSagaGenerationService : IAiSagaGenerationService
{
    private const int MinDeckCount = 2;
    private const int MaxDeckCount = 6;
    private const int MinCardsPerDeck = 5;
    private const int MaxCardsPerDeck = 15;
    private const int MaxPromptLength = 1000;

    private readonly IAiSagaGenerationJobRepository _repository;

    public AiSagaGenerationService(IAiSagaGenerationJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateAiSagaGenerationResult> CreateSagaGenerationJobAsync(
        CreateAiSagaGenerationJobCommand command,
        Guid userId,
        CancellationToken ct = default)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new CreateAiSagaGenerationResult(CreateAiSagaGenerationStatus.Invalid, Error: validationError);
        }

        var now = DateTime.UtcNow;
        var usageWeekStartUtc = GetStartOfWeekUtc(now);
        var existingJob = await _repository.GetForUserWeekAsync(userId, usageWeekStartUtc, ct);
        if (existingJob is not null)
        {
            return new CreateAiSagaGenerationResult(
                CreateAiSagaGenerationStatus.WeeklyLimitExceeded,
                Error: "AI saga generation is limited to one request per week.",
                NextAllowedAtUtc: usageWeekStartUtc.AddDays(7));
        }

        var job = new AiSagaGenerationJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Prompt = command.Prompt.Trim(),
            TargetLanguage = NormalizeOptional(command.TargetLanguage),
            NativeLanguage = NormalizeOptional(command.NativeLanguage),
            Difficulty = NormalizeOptional(command.Difficulty) ?? "Beginner",
            RequestedDeckCount = command.DeckCount,
            RequestedCardsPerDeck = command.CardsPerDeck,
            RequestedMultiChoiceCountPerDeck = command.MultiChoiceCountPerDeck,
            IncludeAudio = command.IncludeAudio,
            Status = AiSagaGenerationStatus.Pending,
            AudioStatus = command.IncludeAudio
                ? AiSagaAudioStatus.Pending
                : AiSagaAudioStatus.NotRequested,
            UsageWeekStartUtc = usageWeekStartUtc,
            CreatedAtUtc = now
        };

        await _repository.AddAsync(job, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (AiSagaGenerationWeeklyLimitExceededException)
        {
            return new CreateAiSagaGenerationResult(
                CreateAiSagaGenerationStatus.WeeklyLimitExceeded,
                Error: "AI saga generation is limited to one request per week.",
                NextAllowedAtUtc: usageWeekStartUtc.AddDays(7));
        }

        return new CreateAiSagaGenerationResult(
            CreateAiSagaGenerationStatus.Created,
            JobId: job.Id,
            NextAllowedAtUtc: usageWeekStartUtc.AddDays(7));
    }

    public async Task<AiSagaGenerationJobDto?> GetSagaGenerationJobAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default)
    {
        var job = await _repository.GetForUserAsync(jobId, userId, ct);

        return job is null
            ? null
            : new AiSagaGenerationJobDto(
                job.Id,
                job.Status.ToString(),
                job.CreatedSagaId,
                job.ErrorMessage,
                job.AudioStatus.ToString(),
                job.RequestedDeckCount,
                job.RequestedCardsPerDeck,
                job.RequestedMultiChoiceCountPerDeck,
                job.UsageWeekStartUtc,
                job.UsageWeekStartUtc.AddDays(7),
                job.CreatedAtUtc,
                job.StartedAtUtc,
                job.CompletedAtUtc);
    }

    private static string? Validate(CreateAiSagaGenerationJobCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
        {
            return "Prompt is required.";
        }

        if (command.Prompt.Trim().Length > MaxPromptLength)
        {
            return "Prompt is too long.";
        }

        if (command.DeckCount is < MinDeckCount or > MaxDeckCount)
        {
            return "Deck count must be between 2 and 6.";
        }

        if (command.CardsPerDeck is < MinCardsPerDeck or > MaxCardsPerDeck)
        {
            return "Cards per deck must be between 5 and 15.";
        }

        if (command.MultiChoiceCountPerDeck < 0 ||
            command.MultiChoiceCountPerDeck > command.CardsPerDeck)
        {
            return "Multi-choice count per deck must be between 0 and the cards per deck count.";
        }

        return null;
    }

    private static DateTime GetStartOfWeekUtc(DateTime value)
    {
        var utcDate = value.ToUniversalTime().Date;
        var diff = ((int)utcDate.DayOfWeek + 6) % 7;
        return utcDate.AddDays(-diff);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
