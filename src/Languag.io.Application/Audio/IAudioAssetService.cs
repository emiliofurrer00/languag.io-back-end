using Languag.io.Domain.Entities;

namespace Languag.io.Application.Audio;

public interface IAudioAssetService
{
    Task<AudioAsset?> GetOrCreateAudioAsync(
        string text,
        string languageCode,
        CancellationToken ct = default);
}
