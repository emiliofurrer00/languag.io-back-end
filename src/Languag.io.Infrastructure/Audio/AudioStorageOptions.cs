namespace Languag.io.Infrastructure.Audio;

public sealed class AudioStorageOptions
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string? SessionToken { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "audio/tts";
}
