using System.Net;
using System.Text;
using System.Text.Json;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.AiDeckGeneration;
using Microsoft.Extensions.Options;

namespace Languag.io.Tests;

public class AiSagaGenerationTests
{
    [Fact]
    public async Task GenerateSagaAsync_AsksOpenAiForSagaNavigationTextInNativeLanguage()
    {
        var handler = new CapturingHandler();
        var generator = new OpenAiSagaGenerator(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.openai.test/v1/")
            },
            Options.Create(new OpenAiDeckGeneratorOptions
            {
                ApiKey = "test-key",
                Model = "test-model"
            }));

        await generator.GenerateSagaAsync(new AiSagaGenerationJob
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Prompt = "office conversation checkpoints",
            TargetLanguage = "Japanese",
            NativeLanguage = "Spanish",
            Difficulty = "Beginner",
            RequestedDeckCount = 2,
            RequestedCardsPerDeck = 5,
            RequestedMultiChoiceCountPerDeck = 1
        });

        Assert.NotNull(handler.RequestBody);
        using var document = JsonDocument.Parse(handler.RequestBody!);
        var userPrompt = document.RootElement
            .GetProperty("input")[1]
            .GetProperty("content")
            .GetString();

        Assert.Contains(
            "Write all saga, chapter, lesson, and deck titles, descriptions, and categories in Spanish.",
            userPrompt);
        Assert.Contains(
            "Treat chapter and lesson titles/descriptions as learner navigation text, not target-language practice.",
            userPrompt);
        Assert.Contains("Target language: Japanese.", userPrompt);
        Assert.Contains("Native language: Spanish.", userPrompt);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(CreateResponseBody(), Encoding.UTF8, "application/json")
            };
        }

        private static string CreateResponseBody()
        {
            var generatedSagaJson = JsonSerializer.Serialize(new
            {
                title = "Conversaciones de oficina",
                description = "Practica frases de oficina.",
                category = "Idiomas",
                chapters = Array.Empty<object>()
            });

            return JsonSerializer.Serialize(new
            {
                output_text = generatedSagaJson
            });
        }
    }
}
