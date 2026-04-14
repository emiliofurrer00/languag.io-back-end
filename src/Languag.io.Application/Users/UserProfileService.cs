namespace Languag.io.Application.Users;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UserProfileService(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return _userProfileRepository.GetByIdAsync(userId, ct);
    }
}
