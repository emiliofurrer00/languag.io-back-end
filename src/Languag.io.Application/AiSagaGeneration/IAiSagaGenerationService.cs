namespace Languag.io.Application.AiSagaGeneration;

public interface IAiSagaGenerationService
{
    Task<CreateAiSagaGenerationResult> CreateSagaGenerationJobAsync(
        CreateAiSagaGenerationJobCommand command,
        Guid userId,
        CancellationToken ct = default);

    Task<AiSagaGenerationJobDto?> GetSagaGenerationJobAsync(
        Guid jobId,
        Guid userId,
        CancellationToken ct = default);
}
