namespace Languag.io.Application.Users;

public interface IUserProfileRepository
{
    Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
