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

    public Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        if (normalizedUsername is null)
        {
            return Task.FromResult(false);
        }

        return _userProfileRepository.IsUsernameAvailableAsync(normalizedUsername, excludingUserId, ct);
    }

    public Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default)
    {
        var normalizedUsername = NormalizeUsername(command.Username);
        if (command.HasBeenOnboarded && normalizedUsername is null)
        {
            return Task.FromResult(new UpdateUserProfileResult(
                UpdateUserProfileStatus.Invalid,
                Error: "A username is required before marking onboarding as complete."));
        }

        var normalizedCommand = command with
        {
            Username = normalizedUsername,
            Name = NormalizeOptionalText(command.Name),
            ProfileDescription = command.ProfileDescription.Trim(),
            About = command.About.Trim()
        };

        return _userProfileRepository.UpdateAsync(normalizedCommand, ct);
    }

    private static string? NormalizeUsername(string? username)
    {
        var normalized = NormalizeOptionalText(username);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
