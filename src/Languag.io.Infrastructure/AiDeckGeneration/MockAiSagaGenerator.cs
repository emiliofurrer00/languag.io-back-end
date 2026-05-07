using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.AiSagaGeneration;
using Languag.io.Domain.Entities;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class MockAiSagaGenerator : IAiSagaGenerator
{
    public Task<GeneratedSagaDto> GenerateSagaAsync(
        AiSagaGenerationJob job,
        CancellationToken ct = default)
    {
        var targetLanguage = string.IsNullOrWhiteSpace(job.TargetLanguage)
            ? "Language"
            : job.TargetLanguage;
        var nativeLanguage = string.IsNullOrWhiteSpace(job.NativeLanguage)
            ? "English"
            : job.NativeLanguage;
        var chapters = Enumerable.Range(1, Math.Min(job.RequestedDeckCount, 3))
            .Select(chapterIndex => new GeneratedSagaChapterDto
            {
                Title = $"Mock {nativeLanguage} Chapter {chapterIndex}",
                Description = $"Mock {nativeLanguage} chapter {chapterIndex} generated for local development."
            })
            .ToList();

        for (var deckIndex = 1; deckIndex <= job.RequestedDeckCount; deckIndex += 1)
        {
            var chapter = chapters[(deckIndex - 1) * chapters.Count / job.RequestedDeckCount];
            chapter.Lessons.Add(new GeneratedSagaLessonDto
            {
                Title = $"Mock {nativeLanguage} lesson {deckIndex}",
                Description = $"Mock {nativeLanguage} practice set {deckIndex} in the generated saga.",
                Deck = CreateDeck(job, targetLanguage, nativeLanguage, deckIndex)
            });
        }

        return Task.FromResult(new GeneratedSagaDto
        {
            Title = $"Mock {nativeLanguage} Saga",
            Description = $"A mock {nativeLanguage} saga generated for local development.",
            Category = "Languages",
            Chapters = chapters
        });
    }

    private static GeneratedDeckDto CreateDeck(
        AiSagaGenerationJob job,
        string targetLanguage,
        string nativeLanguage,
        int deckIndex)
    {
        var multiChoiceCount = Math.Clamp(
            job.RequestedMultiChoiceCountPerDeck,
            0,
            job.RequestedCardsPerDeck);

        return new GeneratedDeckDto
        {
            Title = $"Mock {nativeLanguage} Deck {deckIndex}",
            Description = $"Mock {nativeLanguage} generated deck {deckIndex}.",
            Category = "Languages",
            Cards = Enumerable.Range(1, job.RequestedCardsPerDeck)
                .Select(cardIndex =>
                {
                    var isMultiChoice = cardIndex <= multiChoiceCount;
                    return new GeneratedCardDto
                    {
                        Type = isMultiChoice ? CardTypes.MultiChoice : CardTypes.Flashcard,
                        FrontText = isMultiChoice
                            ? $"What does mock {targetLanguage} saga phrase {deckIndex}.{cardIndex} mean?"
                            : $"Mock {targetLanguage} saga front {deckIndex}.{cardIndex}",
                        BackText = $"Mock saga back {deckIndex}.{cardIndex}",
                        TtsText = $"Mock {targetLanguage} saga front {deckIndex}.{cardIndex}",
                        ExampleSentence = $"Mock saga example sentence {deckIndex}.{cardIndex}.",
                        Choices = isMultiChoice
                            ? CreateMockChoices(deckIndex, cardIndex)
                            : []
                    };
                })
                .ToList()
        };
    }

    private static List<GeneratedCardChoiceDto> CreateMockChoices(int deckIndex, int cardIndex)
    {
        return
        [
            new() { Text = $"Mock saga back {deckIndex}.{cardIndex}", IsCorrect = true },
            new() { Text = $"Mock saga distractor {deckIndex}.{cardIndex}.1", IsCorrect = false },
            new() { Text = $"Mock saga distractor {deckIndex}.{cardIndex}.2", IsCorrect = false },
            new() { Text = $"Mock saga distractor {deckIndex}.{cardIndex}.3", IsCorrect = false }
        ];
    }
}
