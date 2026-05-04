using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Languag.io.Application.Audio;
using Microsoft.Extensions.Options;

namespace Languag.io.Infrastructure.Audio;

public sealed class S3AudioStorageService : IAudioStorageService
{
    private readonly AudioStorageOptions _options;
    private readonly Lazy<IAmazonS3> _s3Client;

    public S3AudioStorageService(IOptions<AudioStorageOptions> options)
    {
        _options = options.Value;
        _s3Client = new Lazy<IAmazonS3>(CreateS3Client);
    }

    public async Task<string> UploadMp3Async(
        Stream audioStream,
        string storageKey,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = storageKey,
            InputStream = audioStream,
            ContentType = "audio/mpeg",
            CannedACL = S3CannedACL.Private
        };

        request.Headers.CacheControl = "public, max-age=31536000, immutable";

        await _s3Client.Value.PutObjectAsync(request, ct);

        return BuildPublicUrl(storageKey);
    }

    public string BuildStorageKey(string languageCode, string textHash)
    {
        var safePrefix = _options.KeyPrefix.Trim('/');
        var safeLanguageCode = languageCode.Trim().Replace("/", "-");

        return $"{safePrefix}/{safeLanguageCode}/{textHash}.mp3";
    }

    private string BuildPublicUrl(string storageKey)
    {
        var safeBaseUrl = _options.PublicBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _options.PublicBaseUrl
            : $"{_options.PublicBaseUrl}/";

        return new Uri(new Uri(safeBaseUrl), storageKey).ToString();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKeyId)
            || string.IsNullOrWhiteSpace(_options.SecretAccessKey)
            || string.IsNullOrWhiteSpace(_options.BucketName)
            || string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Audio storage is not configured. Set AudioStorage credentials, bucket, and public base URL.");
        }
    }

    private IAmazonS3 CreateS3Client()
    {
        EnsureConfigured();

        AWSCredentials credentials = string.IsNullOrWhiteSpace(_options.SessionToken)
            ? new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey)
            : new SessionAWSCredentials(
                _options.AccessKeyId,
                _options.SecretAccessKey,
                _options.SessionToken);

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            return new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = _options.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = _options.Region
            });
        }

        return new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(_options.Region));
    }
}
