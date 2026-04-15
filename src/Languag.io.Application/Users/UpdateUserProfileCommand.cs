namespace Languag.io.Application.Users;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string? Username,
    string? Name,
    bool HasBeenOnboarded,
    int DailyCardsGoal,
    string ProfileDescription,
    string About,
    bool IsPublicProfile);
