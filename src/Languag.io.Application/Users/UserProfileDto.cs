namespace Languag.io.Application.Users;

public sealed record UserProfileDto(
    Guid Id,
    string ExternalId,
    string? Username,
    string? Name,
    string? Email,
    bool HasBeenOnboarded,
    int DailyCardsGoal,
    string AvatarColor,
    string ProfileDescription,
    string About,
    bool IsPublicProfile,
    DateTime CreatedAtUtc,
    IReadOnlyList<UserProfileActivityDto>? RecentActivity = null);
