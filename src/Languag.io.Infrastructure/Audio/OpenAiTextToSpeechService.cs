using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Languag.io.Application.Audio;
using Microsoft.Extensions.Options;

namespace Languag.io.Infrastructure.Audio;

public sealed class OpenAiTextToSpeechService : ITextToSpeechService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OpenAiTextToSpeechOptions _options;

    public OpenAiTextToSpeechService(
        HttpClient httpClient,
        IOptions<OpenAiTextToSpeechOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<Stream> GenerateSpeechAsync(TtsRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY is required when AI TTS is enabled.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "audio/speech");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent(new
        {
            model = _options.Model,
            input = request.Text,
            voice = request.Voice,
            response_format = "mp3",
            speed = decimal.Parse(
                request.Speed.ToString("0.##", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture),
            instructions = request.Instructions
        });

        using var response = await _httpClient.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var responseText = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"OpenAI TTS failed with HTTP {(int)response.StatusCode}: {responseText}");
        }

        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private static StringContent JsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
