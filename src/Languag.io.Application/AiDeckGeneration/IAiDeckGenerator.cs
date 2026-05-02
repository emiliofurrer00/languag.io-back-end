using Languag.io.Domain.Entities;

namespace Languag.io.Application.AiDeckGeneration;

public interface IAiDeckGenerator
{
    Task<GeneratedDeckDto> GenerateDeckAsync(AiDeckGenerationJob job, CancellationToken ct = default);
}
