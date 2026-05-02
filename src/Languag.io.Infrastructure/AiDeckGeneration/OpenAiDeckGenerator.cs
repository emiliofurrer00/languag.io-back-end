using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Languag.io.Application.AiDeckGeneration;
using Languag.io.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Languag.io.Infrastructure.AiDeckGeneration;

public class OpenAiDeckGenerator : IAiDeckGenerator
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiDeckGeneratorOptions _options;

    public OpenAiDeckGenerator(
        HttpClient httpClient,
        IOptions<OpenAiDeckGeneratorOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GeneratedDeckDto> GenerateDeckAsync(
        AiDeckGenerationJob job,
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
                $"OpenAI deck generation failed with HTTP {(int)response.StatusCode}: {responseText}");
        }

        var generatedJson = ExtractOutputText(responseText);
        var deck = JsonSerializer.Deserialize<GeneratedDeckDto>(generatedJson, SerializerOptions);

        return deck ?? throw new InvalidOperationException("OpenAI returned an empty deck payload.");
    }

    private static object CreateRequestPayload(AiDeckGenerationJob job, string model)
    {
        var targetLanguage = job.TargetLanguage ?? "the target language";
        var nativeLanguage = job.NativeLanguage ?? "English";

        return new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "You create concise, accurate language-learning flashcard decks. Return only valid data that matches the schema."
                },
                new
                {
                    role = "user",
                    content = $"""
                    Create a {job.Difficulty} deck with exactly {job.RequestedCardCount} cards.
                    Target language: {targetLanguage}.
                    Native language: {nativeLanguage}.
                    User prompt: {job.Prompt}

                    Put the target-language phrase on frontText and the native-language meaning on backText.
                    Keep cards practical, beginner-safe when relevant, and avoid duplicates.
                    """
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "generated_language_deck",
                    strict = true,
                    schema = new
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
                                minItems = job.RequestedCardCount,
                                maxItems = job.RequestedCardCount,
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    required = new[] { "frontText", "backText", "exampleSentence" },
                                    properties = new
                                    {
                                        frontText = new { type = "string" },
                                        backText = new { type = "string" },
                                        exampleSentence = new { type = "string" }
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
