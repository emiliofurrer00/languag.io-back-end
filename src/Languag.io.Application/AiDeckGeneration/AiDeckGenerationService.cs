using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiDeckGeneration;

public class AiDeckGenerationService : IAiDeckGenerationService
{
    private const int MinCardCount = 5;
    private const int MaxCardCount = 20;
    private const int MaxPromptLength = 1000;

    private readonly IAiDeckGenerationJobRepository _repository;

    public AiDeckGenerationService(IAiDeckGenerationJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateDeckGenerationJobAsync(
        CreateAiDeckGenerationJobCommand command,
        Guid userId,
        CancellationToken ct = default)
    {
        Validate(command);

        var job = new AiDeckGenerationJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Prompt = command.Prompt.Trim(),
            TargetLanguage = NormalizeOptional(command.TargetLanguage),
            NativeLanguage = NormalizeOptional(command.NativeLanguage),
            Difficulty = NormalizeOptional(command.Difficulty) ?? "Beginner",
            RequestedCardCount = command.CardCount,
            RequestedMultiChoiceCount = command.MultiChoiceCount,
            IncludeAudio = command.IncludeAudio,
            Status = AiDeckGenerationStatus.Pending,
            AudioStatus = command.IncludeAudio
                ? AiDeckAudioStatus.Pending
                : AiDeckAudioStatus.NotRequested,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(job, ct);
        await _repository.SaveChangesAsync(ct);

        return job.Id;
    }

    public async Task<AiDeckGenerationJobDto?> GetDeckGenerationJobAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default)
    {
        var job = await _repository.GetForUserAsync(jobId, userId, ct);

        return job is null
            ? null
            : new AiDeckGenerationJobDto(
                job.Id,
                job.Status.ToString(),
                job.CreatedDeckId,
                job.ErrorMessage,
                job.AudioStatus.ToString(),
                job.RequestedCardCount,
                job.RequestedMultiChoiceCount,
                job.CreatedAtUtc,
                job.StartedAtUtc,
                job.CompletedAtUtc);
    }

    private static void Validate(CreateAiDeckGenerationJobCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(command));
        }

        if (command.Prompt.Trim().Length > MaxPromptLength)
        {
            throw new ArgumentException("Prompt is too long.", nameof(command));
        }

        if (command.CardCount is < MinCardCount or > MaxCardCount)
        {
            throw new ArgumentException("Card count must be between 5 and 20.", nameof(command));
        }

        if (command.MultiChoiceCount < 0 || command.MultiChoiceCount > command.CardCount)
        {
            throw new ArgumentException("Multi-choice count must be between 0 and the total card count.", nameof(command));
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
