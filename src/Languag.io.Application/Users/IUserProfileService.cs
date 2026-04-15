namespace Languag.io.Application.Users;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default);
    Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default);
}
