using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Languag.io.Application.AiSagaGeneration;
using Languag.io.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class OpenAiSagaGenerator : IAiSagaGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiDeckGeneratorOptions _options;

    public OpenAiSagaGenerator(
        HttpClient httpClient,
        IOptions<OpenAiDeckGeneratorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GeneratedSagaDto> GenerateSagaAsync(
        AiSagaGenerationJob job,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is required when AI_PROVIDER is OpenAI.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent(CreateRequestPayload(job, _options.Model));

        using var response = await _httpClient.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"OpenAI saga generation failed with HTTP {(int)response.StatusCode}: {responseText}");
        }

        var generatedJson = ExtractOutputText(responseText);
        var saga = JsonSerializer.Deserialize<GeneratedSagaDto>(generatedJson, SerializerOptions);

        return saga ?? throw new InvalidOperationException("OpenAI returned an empty saga payload.");
    }

    private static object CreateRequestPayload(AiSagaGenerationJob job, string model)
    {
        var targetLanguage = job.TargetLanguage ?? "the target language";
        var nativeLanguage = job.NativeLanguage ?? "English";
        var multiChoiceCount = Math.Clamp(
            job.RequestedMultiChoiceCountPerDeck,
            0,
            job.RequestedCardsPerDeck);
        var flashcardCount = job.RequestedCardsPerDeck - multiChoiceCount;

        return new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "You create ordered language-learning sagas made of concise, accurate flashcard decks. Return only valid data that matches the schema."
                },
                new
                {
                    role = "user",
                    content = $"""
                    Create a {job.Difficulty} language-learning saga with exactly {job.RequestedDeckCount} ordered decks.
                    Each deck must contain exactly {job.RequestedCardsPerDeck} cards.
                    In each deck, include exactly {multiChoiceCount} multi-choice cards and exactly {flashcardCount} flashcard cards.
                    Group the decks into 1 to 3 ordered chapters. The total number of lessons across all chapters must equal {job.RequestedDeckCount}.
                    Target language: {targetLanguage}.
                    Native language: {nativeLanguage}.
                    User prompt: {job.Prompt}

                    Make the saga progress from easier to harder concepts, with each lesson/deck building on prior lessons.
                    For each lesson, put a short lesson title and description, then a deck for that lesson.
                    Use type "flashcard" for flashcards and "multi-choice" for multi-choice cards.
                    For flashcards, put the target-language phrase on frontText, the native-language meaning on backText, and an empty choices array.
                    For multi-choice cards, put the question on frontText, the correct answer on backText, and exactly 4 native-language choices with exactly one isCorrect true.
                    Put only the clean target-language speech text in ttsText, with no markdown, numbering, or translation. For multi-choice cards, ttsText should be the target-language phrase being tested.
                    Keep cards practical, safe for learners, and avoid duplicates across the whole saga.
                    """
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "generated_language_saga",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "title", "description", "category", "chapters" },
                        properties = new
                        {
                            title = new { type = "string" },
                            description = new { type = "string" },
                            category = new { type = "string" },
                            chapters = new
                            {
                                type = "array",
                                minItems = 1,
                                maxItems = Math.Min(3, job.RequestedDeckCount),
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "title", "description", "lessons" },
                                    properties = new
                                    {
                                        title = new { type = "string" },
                                        description = new { type = "string" },
                                        lessons = new
                                        {
                                            type = "array",
                                            minItems = 1,
                                            maxItems = job.RequestedDeckCount,
                                            items = new
                                            {
                                                type = "object",
                                                additionalProperties = false,
                                                required = new[] { "title", "description", "deck" },
                                                properties = new
                                                {
                                                    title = new { type = "string" },
                                                    description = new { type = "string" },
                                                    deck = DeckSchema(job)
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static object DeckSchema(AiSagaGenerationJob job)
    {
        return new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "title", "description", "category", "cards" },
            properties = new
            {
                title = new { type = "string" },
                description = new { type = "string" },
                category = new { type = "string" },
                cards = new
                {
                    type = "array",
                    minItems = job.RequestedCardsPerDeck,
                    maxItems = job.RequestedCardsPerDeck,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "type", "frontText", "backText", "ttsText", "exampleSentence", "choices" },
                        properties = new
                        {
                            type = new
                            {
                                type = "string",
                                @enum = new[] { "flashcard", "multi-choice" }
                            },
                            frontText = new { type = "string" },
                            backText = new { type = "string" },
                            ttsText = new { type = "string" },
                            exampleSentence = new { type = "string" },
                            choices = new
                            {
                                type = "array",
                                minItems = 0,
                                maxItems = 4,
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "text", "isCorrect" },
                                    properties = new
                                    {
                                        text = new { type = "string" },
                                        isCorrect = new { type = "boolean" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private static StringContent JsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static string ExtractOutputText(string responseText)
    {
        using var document = JsonDocument.Parse(responseText);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) &&
            outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("output", out var output) &&
            output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) ||
                    content.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }
            }
        }

        throw new InvalidOperationException("OpenAI response did not contain output text.");
    }
}
