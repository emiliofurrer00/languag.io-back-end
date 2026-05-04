namespace Languag.io.Application.Audio;

public interface IAudioStorageService
{
    Task<string> UploadMp3Async(
        Stream audioStream,
        string storageKey,
        CancellationToken ct = default);
}
