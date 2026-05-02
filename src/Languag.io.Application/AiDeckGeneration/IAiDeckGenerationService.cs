namespace Languag.io.Application.AiDeckGeneration;

public interface IAiDeckGenerationService
{
    Task<Guid> CreateDeckGenerationJobAsync(
        CreateAiDeckGenerationJobCommand command,
        Guid userId,
        CancellationToken ct = default);

    Task<AiDeckGenerationJobDto?> GetDeckGenerationJobAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default);
}
