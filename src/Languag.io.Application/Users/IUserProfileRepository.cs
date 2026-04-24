namespace Languag.io.Application.Users;

public interface IUserProfileRepository
{
    Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<PublicUserProfileDto?> GetPublicByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default);
    Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default);
    Task<UpdateUserProfileResult> UpdateProfilePictureObjectKeyAsync(Guid userId, string objectKey, CancellationToken ct = default);
}
