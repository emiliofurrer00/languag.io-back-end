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
        var multiChoiceCount = Math.Clamp(job.RequestedMultiChoiceCount, 0, job.RequestedCardCount);

        var deck = new GeneratedDeckDto
        {
            Title = $"Mock {language} Deck",
            Description = "A mock deck generated for local development.",
            Category = "Languages",
            Cards = Enumerable.Range(1, job.RequestedCardCount)
                .Select(index =>
                {
                    var isMultiChoice = index <= multiChoiceCount;
                    return new GeneratedCardDto
                    {
                        Type = isMultiChoice ? CardTypes.MultiChoice : CardTypes.Flashcard,
                        FrontText = isMultiChoice
                            ? $"What does mock phrase {index} mean?"
                            : $"Mock front {index}",
                        BackText = $"Mock back {index}",
                        TtsText = $"Mock front {index}",
                        ExampleSentence = $"Mock example sentence {index}.",
                        Choices = isMultiChoice
                            ? CreateMockChoices(index)
                            : []
                    };
                })
                .ToList()
        };

        return Task.FromResult(deck);
    }

    private static List<GeneratedCardChoiceDto> CreateMockChoices(int index)
    {
        return
        [
            new() { Text = $"Mock back {index}", IsCorrect = true },
            new() { Text = $"Mock distractor {index}.1", IsCorrect = false },
            new() { Text = $"Mock distractor {index}.2", IsCorrect = false },
            new() { Text = $"Mock distractor {index}.3", IsCorrect = false }
        ];
    }
}
