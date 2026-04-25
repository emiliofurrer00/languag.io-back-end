namespace Languag.io.Application.Users;

public sealed class ProfilePictureService : IProfilePictureService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IProfilePictureStorage _profilePictureStorage;

    public ProfilePictureService(
        IUserProfileRepository userProfileRepository,
        IProfilePictureStorage profilePictureStorage)
    {
        _userProfileRepository = userProfileRepository;
        _profilePictureStorage = profilePictureStorage;
    }

    public async Task<CreateProfilePictureUploadResult> CreateUploadAsync(
        Guid userId,
        string? contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        if (!string.Equals(contentType, IProfilePictureStorage.UploadContentType, StringComparison.OrdinalIgnoreCase))
        {
            return new CreateProfilePictureUploadResult(
                CreateProfilePictureUploadStatus.Invalid,
                Error: "Profile pictures must be uploaded as image/webp.");
        }

        if (contentLength <= 0 || contentLength > _profilePictureStorage.MaxUploadBytes)
        {
            return new CreateProfilePictureUploadResult(
                CreateProfilePictureUploadStatus.Invalid,
                Error: $"Profile pictures must be smaller than {_profilePictureStorage.MaxUploadBytes} bytes.");
        }

        var target = await _profilePictureStorage.CreateUploadTargetAsync(userId, contentLength, ct);

        return new CreateProfilePictureUploadResult(
            CreateProfilePictureUploadStatus.Created,
            Target: target);
    }

    public async Task<CompleteProfilePictureUploadResult> CompleteUploadAsync(
        Guid userId,
        string? objectKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(objectKey) || !_profilePictureStorage.IsOwnedByUser(userId, objectKey))
        {
            return new CompleteProfilePictureUploadResult(
                CompleteProfilePictureUploadStatus.Invalid,
                Error: "Invalid profile picture upload key.");
        }

        var uploadedObject = await _profilePictureStorage.GetObjectAsync(objectKey, ct);
        if (uploadedObject is null)
        {
            return new CompleteProfilePictureUploadResult(
                CompleteProfilePictureUploadStatus.Invalid,
                Error: "The uploaded profile picture could not be found.");
        }

        if (!string.Equals(uploadedObject.ContentType, IProfilePictureStorage.UploadContentType, StringComparison.OrdinalIgnoreCase)
            || uploadedObject.ContentLength <= 0
            || uploadedObject.ContentLength > _profilePictureStorage.MaxUploadBytes
            || !uploadedObject.HasExpectedSignature)
        {
            await _profilePictureStorage.DeleteObjectIfExistsAsync(objectKey, ct);

            return new CompleteProfilePictureUploadResult(
                CompleteProfilePictureUploadStatus.Invalid,
                Error: "The uploaded profile picture did not pass validation.");
        }

        var updateResult = await _userProfileRepository.UpdateProfilePictureObjectKeyAsync(userId, objectKey, ct);
        if (updateResult.Status != UpdateUserProfileStatus.Updated)
        {
            await _profilePictureStorage.DeleteObjectIfExistsAsync(objectKey, ct);

            return updateResult.Status == UpdateUserProfileStatus.NotFound
                ? new CompleteProfilePictureUploadResult(CompleteProfilePictureUploadStatus.NotFound)
                : new CompleteProfilePictureUploadResult(
                    CompleteProfilePictureUploadStatus.Invalid,
                    Error: updateResult.Error);
        }

        if (!string.IsNullOrWhiteSpace(updateResult.PreviousProfilePictureObjectKey)
            && !string.Equals(updateResult.PreviousProfilePictureObjectKey, objectKey, StringComparison.Ordinal))
        {
            await _profilePictureStorage.DeleteObjectIfExistsAsync(updateResult.PreviousProfilePictureObjectKey, ct);
        }

        return new CompleteProfilePictureUploadResult(
            CompleteProfilePictureUploadStatus.Updated,
            updateResult.Profile);
    }
}
