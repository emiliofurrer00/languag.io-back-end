using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiSagaGeneration;

public interface IAiSagaGenerator
{
    Task<GeneratedSagaDto> GenerateSagaAsync(AiSagaGenerationJob job, CancellationToken ct = default);
}
