namespace Languag.io.Application.Users;

public enum UpdateUserProfileStatus
{
    Updated,
    NotFound,
    UsernameTaken,
    Invalid
}

public sealed record UpdateUserProfileResult(
    UpdateUserProfileStatus Status,
    UserProfileDto? Profile = null,
    string? PreviousProfilePictureObjectKey = null,
    string? Error = null);
