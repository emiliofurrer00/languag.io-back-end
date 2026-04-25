using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Languag.io.Application.Users;
using Microsoft.Extensions.Configuration;

namespace Languag.io.Infrastructure.Storage;

public sealed class S3ProfilePictureStorage : IProfilePictureStorage
{
    private const int WebpSignatureLength = 12;
    private const string AwsAlgorithm = "AWS4-HMAC-SHA256";
    private const string AwsService = "s3";
    private readonly string? _accessKeyId;
    private readonly string? _secretAccessKey;
    private readonly string? _sessionToken;
    private readonly string? _bucketName;
    private readonly string _region;
    private readonly string _keyPrefix;
    private readonly string? _cloudFrontBaseUrl;
    private readonly TimeSpan _uploadTtl;
    private readonly Lazy<IAmazonS3> _s3Client;

    public S3ProfilePictureStorage(IConfiguration configuration)
    {
        _accessKeyId = ReadConfig(configuration, "ProfileImages:AccessKeyId", "AWS_ACCESS_KEY_ID");
        _secretAccessKey = ReadConfig(configuration, "ProfileImages:SecretAccessKey", "AWS_SECRET_ACCESS_KEY");
        _sessionToken = ReadConfig(configuration, "ProfileImages:SessionToken", "AWS_SESSION_TOKEN");
        _bucketName = ReadConfig(configuration, "ProfileImages:BucketName", "AWS_S3_BUCKET", "AWS_BUCKET_NAME");
        _region = ReadConfig(configuration, "ProfileImages:Region", "AWS_REGION") ?? "us-east-1";
        _keyPrefix = (ReadConfig(configuration, "ProfileImages:KeyPrefix") ?? "profile-pictures").Trim('/');
        _cloudFrontBaseUrl = ReadConfig(
            configuration,
            "ProfileImages:CloudFrontBaseUrl",
            "CLOUDFRONT_BASE_URL",
            "AWS_CLOUDFRONT_BASE_URL");
        MaxUploadBytes = ReadLongConfig(configuration, "ProfileImages:MaxUploadBytes", 512 * 1024);
        _uploadTtl = TimeSpan.FromSeconds(ReadLongConfig(configuration, "ProfileImages:UploadTtlSeconds", 120));
        _s3Client = new Lazy<IAmazonS3>(CreateS3Client);
    }

    public long MaxUploadBytes { get; }

    public bool IsOwnedByUser(Guid userId, string objectKey)
    {
        return objectKey.StartsWith(BuildUserPrefix(userId), StringComparison.Ordinal);
    }

    public Task<ProfilePictureUploadTarget> CreateUploadTargetAsync(
        Guid userId,
        long contentLength,
        CancellationToken ct = default)
    {
        EnsureConfigured();
        ct.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var expiresAtUtc = now.Add(_uploadTtl);
        var objectKey = $"{BuildUserPrefix(userId)}{Guid.NewGuid():N}.webp";
        var credential = $"{_accessKeyId}/{dateStamp}/{_region}/{AwsService}/aws4_request";
        var fields = new Dictionary<string, string>
        {
            ["key"] = objectKey,
            ["Content-Type"] = IProfilePictureStorage.UploadContentType,
            ["success_action_status"] = "204",
            ["x-amz-algorithm"] = AwsAlgorithm,
            ["x-amz-credential"] = credential,
            ["x-amz-date"] = amzDate
        };

        if (!string.IsNullOrWhiteSpace(_sessionToken))
        {
            fields["x-amz-security-token"] = _sessionToken;
        }

        var conditions = new List<object>
        {
            new Dictionary<string, string> { ["bucket"] = _bucketName! },
            new Dictionary<string, string> { ["key"] = objectKey },
            new Dictionary<string, string> { ["Content-Type"] = IProfilePictureStorage.UploadContentType },
            new Dictionary<string, string> { ["success_action_status"] = "204" },
            new Dictionary<string, string> { ["x-amz-algorithm"] = AwsAlgorithm },
            new Dictionary<string, string> { ["x-amz-credential"] = credential },
            new Dictionary<string, string> { ["x-amz-date"] = amzDate },
            new object[] { "content-length-range", 1, MaxUploadBytes }
        };

        if (!string.IsNullOrWhiteSpace(_sessionToken))
        {
            conditions.Add(new Dictionary<string, string> { ["x-amz-security-token"] = _sessionToken });
        }

        var policy = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            expiration = expiresAtUtc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            conditions
        }));
        fields["policy"] = policy;
        fields["x-amz-signature"] = SignPolicy(policy, dateStamp);

        return Task.FromResult(new ProfilePictureUploadTarget(
            BuildS3PostUrl(),
            fields,
            objectKey,
            BuildPublicUrl(objectKey),
            expiresAtUtc,
            MaxUploadBytes));
    }

    public async Task<UploadedProfilePictureObject?> GetObjectAsync(
        string objectKey,
        CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            var response = await _s3Client.Value.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            }, ct);

            return new UploadedProfilePictureObject(
                objectKey,
                response.Headers.ContentType,
                response.Headers.ContentLength,
                await HasWebpSignatureAsync(objectKey, ct));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteObjectIfExistsAsync(string objectKey, CancellationToken ct = default)
    {
        EnsureConfigured();

        try
        {
            await _s3Client.Value.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            }, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    public string? BuildPublicUrl(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || string.IsNullOrWhiteSpace(_cloudFrontBaseUrl))
        {
            return null;
        }

        var safeBaseUrl = _cloudFrontBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? _cloudFrontBaseUrl
            : $"{_cloudFrontBaseUrl}/";

        return new Uri(new Uri(safeBaseUrl), objectKey).ToString();
    }

    private static string? ReadConfig(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static long ReadLongConfig(IConfiguration configuration, string key, long fallback)
    {
        var value = configuration[key];
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0
            ? parsed
            : fallback;
    }

    private string BuildUserPrefix(Guid userId)
    {
        return $"{_keyPrefix}/{userId:N}/";
    }

    private string BuildS3PostUrl()
    {
        return _region.Equals("us-east-1", StringComparison.OrdinalIgnoreCase)
            ? $"https://{_bucketName}.s3.amazonaws.com/"
            : $"https://{_bucketName}.s3.{_region}.amazonaws.com/";
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_accessKeyId)
            || string.IsNullOrWhiteSpace(_secretAccessKey)
            || string.IsNullOrWhiteSpace(_bucketName)
            || string.IsNullOrWhiteSpace(_cloudFrontBaseUrl))
        {
            throw new InvalidOperationException(
                "Profile image storage is not configured. Set AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION, AWS_S3_BUCKET, and CLOUDFRONT_BASE_URL.");
        }
    }

    private IAmazonS3 CreateS3Client()
    {
        EnsureConfigured();

        AWSCredentials credentials = string.IsNullOrWhiteSpace(_sessionToken)
            ? new BasicAWSCredentials(_accessKeyId, _secretAccessKey)
            : new SessionAWSCredentials(_accessKeyId, _secretAccessKey, _sessionToken);

        return new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(_region));
    }

    private async Task<bool> HasWebpSignatureAsync(string objectKey, CancellationToken ct)
    {
        try
        {
            using var response = await _s3Client.Value.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            }, ct);

            var buffer = new byte[WebpSignatureLength];
            var totalRead = 0;
            while (totalRead < buffer.Length)
            {
                var read = await response.ResponseStream.ReadAsync(
                    buffer.AsMemory(totalRead, buffer.Length - totalRead),
                    ct);

                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return totalRead == WebpSignatureLength
                && buffer[0] == 'R'
                && buffer[1] == 'I'
                && buffer[2] == 'F'
                && buffer[3] == 'F'
                && buffer[8] == 'W'
                && buffer[9] == 'E'
                && buffer[10] == 'B'
                && buffer[11] == 'P';
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private string SignPolicy(string policy, string dateStamp)
    {
        var dateKey = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{_secretAccessKey}"), dateStamp);
        var dateRegionKey = HmacSha256(dateKey, _region);
        var dateRegionServiceKey = HmacSha256(dateRegionKey, AwsService);
        var signingKey = HmacSha256(dateRegionServiceKey, "aws4_request");
        return Convert.ToHexString(HmacSha256(signingKey, policy)).ToLowerInvariant();
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));
    }
}
