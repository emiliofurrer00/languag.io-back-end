namespace Languag.io.Application.Users;

public sealed record UserProfileDto(
    Guid Id,
    string ExternalId,
    string? Name,
    string? Email,
    string ProfileDescription,
    string About,
    bool IsPublicProfile);
