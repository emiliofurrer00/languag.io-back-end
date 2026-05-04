namespace Languag.io.Application.Audio;

public interface ITextToSpeechService
{
    Task<Stream> GenerateSpeechAsync(TtsRequest request, CancellationToken ct = default);
}

public sealed record TtsRequest(
    string Text,
    string LanguageCode,
    string Voice,
    decimal Speed,
    string Instructions);
