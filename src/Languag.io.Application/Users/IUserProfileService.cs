namespace Languag.io.Application.Users;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
