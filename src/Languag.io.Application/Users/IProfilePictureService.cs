namespace Languag.io.Application.Users;

public interface IProfilePictureService
{
    Task<CreateProfilePictureUploadResult> CreateUploadAsync(
        Guid userId,
        string? contentType,
        long contentLength,
        CancellationToken ct = default);

    Task<CompleteProfilePictureUploadResult> CompleteUploadAsync(
        Guid userId,
        string? objectKey,
        CancellationToken ct = default);
}
