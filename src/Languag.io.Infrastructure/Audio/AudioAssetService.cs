using Languag.io.Application.Audio;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Languag.io.Infrastructure.Audio;

public sealed class AudioAssetService : IAudioAssetService
{
    public const int MaxTtsTextLength = 250;

    private const string Provider = "openai";

    private readonly AppDbContext _dbContext;
    private readonly ITextToSpeechService _tts;
    private readonly IAudioStorageService _storage;
    private readonly OpenAiTextToSpeechOptions _ttsOptions;
    private readonly ILogger<AudioAssetService> _logger;

    public AudioAssetService(
        AppDbContext dbContext,
        ITextToSpeechService tts,
        IAudioStorageService storage,
        IOptions<OpenAiTextToSpeechOptions> ttsOptions,
        ILogger<AudioAssetService> logger)
    {
        _dbContext = dbContext;
        _tts = tts;
        _storage = storage;
        _ttsOptions = ttsOptions.Value;
        _logger = logger;
    }

    public async Task<AudioAsset?> GetOrCreateAudioAsync(
        string text,
        string languageCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length > MaxTtsTextLength)
        {
            return null;
        }

        var normalizedText = AudioHashing.NormalizeForAudio(text);
        var instructionsVersion = _ttsOptions.InstructionsVersion;
        var textHash = AudioHashing.ComputeAudioHash(
            normalizedText,
            languageCode,
            Provider,
            _ttsOptions.Model,
            _ttsOptions.Voice,
            _ttsOptions.Speed,
            instructionsVersion);

        var existing = await _dbContext.AudioAssets
            .FirstOrDefaultAsync(asset => asset.TextHash == textHash, ct);

        if (existing is not null && existing.Status != AudioAssetStatus.Failed)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var asset = existing ?? new AudioAsset
        {
            Id = Guid.NewGuid(),
            TextHash = textHash,
            NormalizedText = normalizedText,
            LanguageCode = languageCode.Trim(),
            Provider = Provider,
            Model = _ttsOptions.Model,
            Voice = _ttsOptions.Voice,
            Speed = _ttsOptions.Speed,
            InstructionsHash = AudioHashing.ComputeInstructionsHash(instructionsVersion),
            StorageKey = BuildStorageKey(languageCode, textHash),
            CreatedAtUtc = now
        };

        asset.Status = AudioAssetStatus.Processing;
        asset.PublicUrl = string.Empty;
        asset.UpdatedAtUtc = now;

        if (existing is null)
        {
            _dbContext.AudioAssets.Add(asset);
        }

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (existing is null)
            {
                _dbContext.Entry(asset).State = EntityState.Detached;
            }

            var duplicate = await _dbContext.AudioAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.TextHash == textHash, ct);

            if (duplicate is not null)
            {
                return duplicate;
            }

            throw;
        }

        try
        {
            var audioStream = await _tts.GenerateSpeechAsync(
                new TtsRequest(
                    text.Trim(),
                    languageCode.Trim(),
                    _ttsOptions.Voice,
                    _ttsOptions.Speed,
                    BuildInstructions(languageCode)),
                ct);

            await using (audioStream)
            {
                asset.PublicUrl = await _storage.UploadMp3Async(audioStream, asset.StorageKey, ct);
            }

            asset.Status = AudioAssetStatus.Ready;
            asset.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            return asset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS generation failed for audio asset {AudioAssetId}.", asset.Id);

            asset.Status = AudioAssetStatus.Failed;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            return asset;
        }
    }

    private string BuildStorageKey(string languageCode, string textHash)
    {
        if (_storage is S3AudioStorageService s3Storage)
        {
            return s3Storage.BuildStorageKey(languageCode, textHash);
        }

        var safeLanguageCode = languageCode.Trim().Replace("/", "-");
        return $"audio/tts/{safeLanguageCode}/{textHash}.mp3";
    }

    private static string BuildInstructions(string languageCode)
    {
        return $"Speak clearly and naturally in {languageCode}. Use a slightly slower pace for a beginner language learner.";
    }
}
