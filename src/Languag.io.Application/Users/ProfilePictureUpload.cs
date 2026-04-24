namespace Languag.io.Application.Users;

public sealed record ProfilePictureUploadTarget(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string ObjectKey,
    string? PublicUrl,
    DateTime ExpiresAtUtc,
    long MaxBytes);

public sealed record UploadedProfilePictureObject(
    string ObjectKey,
    string? ContentType,
    long ContentLength);

public enum CreateProfilePictureUploadStatus
{
    Created,
    Invalid
}

public sealed record CreateProfilePictureUploadResult(
    CreateProfilePictureUploadStatus Status,
    ProfilePictureUploadTarget? Target = null,
    string? Error = null);

public enum CompleteProfilePictureUploadStatus
{
    Updated,
    NotFound,
    Invalid
}

public sealed record CompleteProfilePictureUploadResult(
    CompleteProfilePictureUploadStatus Status,
    UserProfileDto? Profile = null,
    string? Error = null);

public interface IProfilePictureUrlBuilder
{
    string? BuildPublicUrl(string? objectKey);
}

public interface IProfilePictureStorage : IProfilePictureUrlBuilder
{
    const string UploadContentType = "image/webp";

    long MaxUploadBytes { get; }

    bool IsOwnedByUser(Guid userId, string objectKey);

    Task<ProfilePictureUploadTarget> CreateUploadTargetAsync(
        Guid userId,
        long contentLength,
        CancellationToken ct = default);

    Task<UploadedProfilePictureObject?> GetObjectAsync(
        string objectKey,
        CancellationToken ct = default);

    Task DeleteObjectIfExistsAsync(string objectKey, CancellationToken ct = default);
}
