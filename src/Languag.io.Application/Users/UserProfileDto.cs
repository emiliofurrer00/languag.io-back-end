namespace Languag.io.Application.Users;

public sealed record UserProfileDto(
    Guid Id,
    string ExternalId,
    string? Username,
    string? Name,
    string? Email,
    bool HasBeenOnboarded,
    int DailyCardsGoal,
    string ProfileDescription,
    string About,
    bool IsPublicProfile);
