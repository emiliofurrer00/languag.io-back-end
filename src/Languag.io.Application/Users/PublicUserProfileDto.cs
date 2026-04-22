namespace Languag.io.Application.Users;

public sealed record PublicUserProfileDto(
    Guid Id,
    string Username,
    string? Name,
    string AvatarColor,
    string ProfileDescription,
    string About,
    bool IsPublicProfile,
    DateTime CreatedAtUtc,
    IReadOnlyList<UserProfileActivityDto>? RecentActivity = null,
    UserProfileStatsDto? Stats = null);
