namespace Languag.io.Application.Users;

public sealed record UserProfileStatsDto(
    int DecksCreated,
    int CardsStudied,
    int MasteredDecks,
    int StudyStreakDays);
