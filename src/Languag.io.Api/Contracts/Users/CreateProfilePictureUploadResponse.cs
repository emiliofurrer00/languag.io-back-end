namespace Languag.io.Api.Contracts.Users;

public sealed record CreateProfilePictureUploadResponse(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Fields,
    string ObjectKey,
    string? PublicUrl,
    DateTime ExpiresAtUtc,
    long MaxBytes);
