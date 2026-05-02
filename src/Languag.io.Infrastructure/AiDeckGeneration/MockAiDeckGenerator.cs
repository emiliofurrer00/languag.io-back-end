using Languag.io.Application.AiDeckGeneration;
using Languag.io.Domain.Entities;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class MockAiDeckGenerator : IAiDeckGenerator
{
    public Task<GeneratedDeckDto> GenerateDeckAsync(
        AiDeckGenerationJob job,
        CancellationToken ct = default)
    {
        var language = string.IsNullOrWhiteSpace(job.TargetLanguage)
            ? "Language"
            : job.TargetLanguage;

        var deck = new GeneratedDeckDto
        {
            Title = $"Mock {language} Deck",
            Description = "A mock deck generated for local development.",
            Category = "Languages",
            Cards = Enumerable.Range(1, job.RequestedCardCount)
                .Select(index => new GeneratedCardDto
                {
                    FrontText = $"Mock front {index}",
                    BackText = $"Mock back {index}",
                    ExampleSentence = $"Mock example sentence {index}."
                })
                .ToList()
        };

        return Task.FromResult(deck);
    }
}
