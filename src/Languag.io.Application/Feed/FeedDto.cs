namespace Languag.io.Application.Feed;

public sealed record FeedDto(
    FeedDailyGoalDto DailyGoal,
    FeedStreakDto Streak,
    FeedSummaryDto Summary,
    IReadOnlyList<FeedContinueStudyingDeckDto> ContinueStudying,
    IReadOnlyList<FeedActivityDto> FriendsActivity,
    IReadOnlyList<FeedSuggestedPersonDto> SuggestedPeople,
    IReadOnlyList<FeedSuggestedDeckDto> SuggestedDecks);

public sealed record FeedDailyGoalDto(
    int Goal,
    int Progress,
    int Percentage);

public sealed record FeedStreakDto(
    int Current,
    IReadOnlyList<FeedStreakDayDto> Days);

public sealed record FeedStreakDayDto(
    string Day,
    bool Done);

public sealed record FeedSummaryDto(
    string? League,
    int Decks,
    int Cards);

public sealed record FeedContinueStudyingDeckDto(
    Guid Id,
    string Title,
    int Cards,
    int Progress,
    string Color,
    string LastStudied,
    DateTime? LastStudiedAtUtc);

public sealed record FeedActivityDto(
    Guid UserId,
    string? Username,
    string User,
    string Avatar,
    string Color,
    string Action,
    string? Target,
    string Time,
    DateTime OccurredAtUtc,
    bool FollowsYou,
    bool IsFollowing);

public sealed record FeedSuggestedPersonDto(
    Guid UserId,
    string Username,
    string Name,
    string Handle,
    string Avatar,
    string Color,
    string Bio,
    string FriendshipStatus);

public sealed record FeedSuggestedDeckDto(
    Guid Id,
    string Title,
    int Cards,
    string Category,
    string Color,
    int Progress,
    string? OwnerUsername);
