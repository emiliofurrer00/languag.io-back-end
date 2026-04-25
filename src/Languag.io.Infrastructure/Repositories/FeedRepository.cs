using Languag.io.Application.Feed;
using Languag.io.Application.Friends;
using Languag.io.Application.Users;
using Languag.io.Domain.Entities;
using Languag.io.Domain.Enums;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Repositories;

public sealed class FeedRepository : IFeedRepository
{
    private const int ContinueStudyingLimit = 5;
    private const int ActivityLimit = 12;
    private const int SuggestedPeopleLimit = 6;
    private const int SuggestedDecksLimit = 6;

    private static readonly string[] OrderedDayLabels = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
    private static readonly HashSet<string> AllowedColors = ["yellow", "teal", "magenta", "coral", "blue"];

    private readonly AppDbContext _dbContext;
    private readonly IProfilePictureUrlBuilder _profilePictureUrlBuilder;

    public FeedRepository(AppDbContext dbContext, IProfilePictureUrlBuilder profilePictureUrlBuilder)
    {
        _dbContext = dbContext;
        _profilePictureUrlBuilder = profilePictureUrlBuilder;
    }

    public async Task<FeedDto?> GetFeedAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(existingUser => existingUser.Id == currentUserId)
            .Select(existingUser => new
            {
                existingUser.Id,
                existingUser.DailyCardsGoal
            })
            .SingleOrDefaultAsync(ct);

        if (user is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var completedCardsToday = await _dbContext.StudySessionResponses
            .AsNoTracking()
            .CountAsync(
                response => response.UserId == currentUserId
                    && response.StudySession.CreatedAtUtc >= todayStart
                    && response.StudySession.CreatedAtUtc < tomorrowStart,
                ct);

        var dailyGoal = new FeedDailyGoalDto(
            Goal: user.DailyCardsGoal,
            Progress: completedCardsToday,
            Percentage: CalculatePercentage(completedCardsToday, user.DailyCardsGoal));

        var studySessionTimestamps = await _dbContext.StudySessions
            .AsNoTracking()
            .Where(studySession => studySession.UserId == currentUserId)
            .Select(studySession => studySession.CreatedAtUtc)
            .ToListAsync(ct);

        var studiedDays = studySessionTimestamps
            .Select(timestamp => timestamp.Date)
            .ToHashSet();

        var streak = new FeedStreakDto(
            Current: CalculateCurrentStreak(studiedDays, todayStart),
            Days: BuildWeekDays(studiedDays, todayStart));

        var summary = new FeedSummaryDto(
            League: null,
            Decks: await _dbContext.Decks
                .AsNoTracking()
                .CountAsync(deck => deck.OwnerId == currentUserId, ct),
            Cards: await _dbContext.StudySessionResponses
                .AsNoTracking()
                .CountAsync(response => response.UserId == currentUserId, ct));

        var continueStudying = await BuildContinueStudyingAsync(currentUserId, now, ct);
        var friendIds = await GetFriendIdsAsync(currentUserId, ct);
        var friendsActivity = await BuildFriendsActivityAsync(friendIds, now, ct);
        var excludedSuggestedUserIds = await BuildExcludedSuggestedUserIdsAsync(currentUserId, friendIds, ct);
        var suggestedPeople = await BuildSuggestedPeopleAsync(excludedSuggestedUserIds, ct);
        var suggestedDecks = await BuildSuggestedDecksAsync(currentUserId, ct);

        return new FeedDto(
            dailyGoal,
            streak,
            summary,
            continueStudying,
            friendsActivity,
            suggestedPeople,
            suggestedDecks);
    }

    private async Task<IReadOnlyList<FeedContinueStudyingDeckDto>> BuildContinueStudyingAsync(
        Guid currentUserId,
        DateTime now,
        CancellationToken ct)
    {
        var recentSessions = await _dbContext.StudySessions
            .AsNoTracking()
            .Where(studySession => studySession.UserId == currentUserId)
            .OrderByDescending(studySession => studySession.CreatedAtUtc)
            .Select(studySession => new
            {
                studySession.DeckId,
                studySession.CreatedAtUtc,
                studySession.PercentageCorrect,
                DeckTitle = studySession.Deck.Title,
                DeckColor = studySession.Deck.Color,
                Cards = studySession.Deck.Cards.Count
            })
            .Take(50)
            .ToListAsync(ct);

        return recentSessions
            .GroupBy(studySession => studySession.DeckId)
            .Select(group => group.First())
            .Take(ContinueStudyingLimit)
            .Select(studySession => new FeedContinueStudyingDeckDto(
                Id: studySession.DeckId,
                Title: studySession.DeckTitle,
                Cards: studySession.Cards,
                Progress: ClampPercentage(studySession.PercentageCorrect),
                Color: ToColorClass(studySession.DeckColor, "yellow"),
                LastStudied: FormatStudyRecency(studySession.CreatedAtUtc, now),
                LastStudiedAtUtc: studySession.CreatedAtUtc))
            .ToArray();
    }

    private async Task<List<Guid>> GetFriendIdsAsync(Guid currentUserId, CancellationToken ct)
    {
        var asUser1 = _dbContext.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.User1Id == currentUserId)
            .Select(friendship => friendship.User2Id);

        var asUser2 = _dbContext.Friendships
            .AsNoTracking()
            .Where(friendship => friendship.User2Id == currentUserId)
            .Select(friendship => friendship.User1Id);

        return await asUser1
            .Concat(asUser2)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<FeedActivityDto>> BuildFriendsActivityAsync(
        List<Guid> friendIds,
        DateTime now,
        CancellationToken ct)
    {
        if (friendIds.Count == 0)
        {
            return [];
        }

        var activities = await _dbContext.ActivityLogs
            .AsNoTracking()
            .Where(activity => friendIds.Contains(activity.UserId))
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .Take(ActivityLimit)
            .Select(activity => new
            {
                activity.UserId,
                Username = activity.User.Username,
                Name = activity.User.Name,
                Email = activity.User.Email,
                ExternalId = activity.User.ExternalId,
                AvatarColor = activity.User.AvatarColor,
                activity.User.ProfilePictureObjectKey,
                activity.Type,
                activity.StreakDays,
                activity.Metadata,
                DeckTitle = activity.Deck != null ? activity.Deck.Title : null,
                activity.OccurredAtUtc
            })
            .ToListAsync(ct);

        return activities
            .Select(activity =>
            {
                var (action, target) = MapActivity(activity.Type, activity.DeckTitle, activity.StreakDays, activity.Metadata);
                var displayName = BuildFeedDisplayName(activity.Username, activity.Name, activity.Email, activity.ExternalId);
                return new FeedActivityDto(
                    UserId: activity.UserId,
                    Username: activity.Username,
                    User: displayName,
                    Avatar: BuildAvatar(displayName, activity.Username),
                    ProfilePictureUrl: _profilePictureUrlBuilder.BuildPublicUrl(activity.ProfilePictureObjectKey),
                    Color: ToColorClass(activity.AvatarColor),
                    Action: action,
                    Target: target,
                    Time: FormatRelativeTime(activity.OccurredAtUtc, now),
                    OccurredAtUtc: activity.OccurredAtUtc,
                    FollowsYou: true,
                    IsFollowing: true);
            })
            .ToArray();
    }

    private async Task<List<Guid>> BuildExcludedSuggestedUserIdsAsync(
        Guid currentUserId,
        List<Guid> friendIds,
        CancellationToken ct)
    {
        var pendingParticipants = await _dbContext.FriendRequests
            .AsNoTracking()
            .Where(request => request.Status == FriendRequestStatus.Pending
                && (request.SenderId == currentUserId || request.ReceiverId == currentUserId))
            .Select(request => new
            {
                request.SenderId,
                request.ReceiverId
            })
            .ToListAsync(ct);

        var excludedIds = new HashSet<Guid>(friendIds)
        {
            currentUserId
        };

        foreach (var request in pendingParticipants)
        {
            excludedIds.Add(request.SenderId == currentUserId ? request.ReceiverId : request.SenderId);
        }

        return [.. excludedIds];
    }

    private async Task<IReadOnlyList<FeedSuggestedPersonDto>> BuildSuggestedPeopleAsync(
        List<Guid> excludedUserIds,
        CancellationToken ct)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsPublicProfile
                && user.Username != null
                && user.Username != string.Empty
                && !excludedUserIds.Contains(user.Id))
            .OrderByDescending(user => user.CreatedAtUtc)
            .Take(SuggestedPeopleLimit)
            .Select(user => new
            {
                user.Id,
                Username = user.Username!,
                user.Name,
                user.AvatarColor,
                user.ProfileDescription,
                user.About,
                user.Email,
                user.ExternalId,
                user.ProfilePictureObjectKey
            })
            .ToListAsync(ct);

        return users
            .Select(user =>
            {
                var displayName = BuildFeedDisplayName(user.Username, user.Name, user.Email, user.ExternalId);
                return new FeedSuggestedPersonDto(
                    UserId: user.Id,
                    Username: user.Username,
                    Name: displayName,
                    Handle: user.Username,
                    Avatar: BuildAvatar(displayName, user.Username),
                    ProfilePictureUrl: _profilePictureUrlBuilder.BuildPublicUrl(user.ProfilePictureObjectKey),
                    Color: ToColorClass(user.AvatarColor),
                    Bio: BuildBio(user.ProfileDescription, user.About),
                    FriendshipStatus: FriendshipStatuses.None);
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<FeedSuggestedDeckDto>> BuildSuggestedDecksAsync(Guid currentUserId, CancellationToken ct)
    {
        var decks = await _dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.Visibility == DeckVisibility.Public && deck.OwnerId != currentUserId)
            .OrderByDescending(deck => deck.UpdatedAtUtc)
            .Take(SuggestedDecksLimit)
            .Select(deck => new
            {
                deck.Id,
                deck.Title,
                deck.Category,
                deck.Color,
                Cards = deck.Cards.Count,
                OwnerUsername = deck.User != null ? deck.User.Username : null
            })
            .ToListAsync(ct);

        return decks
            .Select(deck => new FeedSuggestedDeckDto(
                Id: deck.Id,
                Title: deck.Title,
                Cards: deck.Cards,
                Category: string.IsNullOrWhiteSpace(deck.Category) ? "General" : deck.Category,
                Color: ToColorClass(deck.Color, "coral"),
                Progress: 0,
                OwnerUsername: deck.OwnerUsername))
            .ToArray();
    }

    private static int CalculatePercentage(int progress, int goal)
    {
        if (goal <= 0)
        {
            return 0;
        }

        var percentage = (int)Math.Round(progress * 100d / goal, MidpointRounding.AwayFromZero);
        return Math.Clamp(percentage, 0, 100);
    }

    private static int CalculateCurrentStreak(HashSet<DateTime> studiedDays, DateTime today)
    {
        if (studiedDays.Count == 0)
        {
            return 0;
        }

        var streak = 0;
        var cursor = studiedDays.Contains(today) ? today : today.AddDays(-1);

        while (studiedDays.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static IReadOnlyList<FeedStreakDayDto> BuildWeekDays(HashSet<DateTime> studiedDays, DateTime today)
    {
        var startOfWeek = GetStartOfWeek(today);
        var days = new List<FeedStreakDayDto>(OrderedDayLabels.Length);

        for (var index = 0; index < OrderedDayLabels.Length; index++)
        {
            var day = startOfWeek.AddDays(index);
            days.Add(new FeedStreakDayDto(OrderedDayLabels[index], studiedDays.Contains(day)));
        }

        return days;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }

    private static int ClampPercentage(decimal percentage)
    {
        var rounded = (int)Math.Round(percentage, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, 100);
    }

    private static string BuildFeedDisplayName(string? username, string? name, string? email, string externalId)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            return username.Trim();
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return email.Trim();
        }

        return externalId;
    }

    private static string BuildAvatar(string displayName, string? username)
    {
        var source = !string.IsNullOrWhiteSpace(displayName) ? displayName : username;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "U";
        }

        var parts = source
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToArray();

        if (parts.Length == 0)
        {
            return source[..1].ToUpperInvariant();
        }

        if (parts.Length == 1)
        {
            var segment = parts[0];
            return segment.Length == 1
                ? segment.ToUpperInvariant()
                : segment[..Math.Min(2, segment.Length)].ToUpperInvariant();
        }

        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
    }

    private static string BuildBio(string profileDescription, string about)
    {
        if (!string.IsNullOrWhiteSpace(profileDescription))
        {
            return profileDescription.Trim();
        }

        if (!string.IsNullOrWhiteSpace(about))
        {
            return about.Trim();
        }

        return "Learning with Languag.io";
    }

    private static string ToColorClass(string? value, string fallback = "teal")
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var trimmed = value.Trim();
            if (trimmed.StartsWith("bg-", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var normalized = trimmed
                .ToLowerInvariant()
                .Replace("bg-", string.Empty, StringComparison.Ordinal)
                .Replace("neo-", string.Empty, StringComparison.Ordinal);

            if (AllowedColors.Contains(normalized))
            {
                return $"bg-neo-{normalized}";
            }
        }

        return $"bg-neo-{fallback}";
    }

    private static string FormatRelativeTime(DateTime value, DateTime now)
    {
        var diff = now - value;
        if (diff < TimeSpan.Zero)
        {
            diff = TimeSpan.Zero;
        }

        if (diff < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (diff < TimeSpan.FromHours(1))
        {
            return $"{Math.Max(1, (int)diff.TotalMinutes)}m ago";
        }

        if (diff < TimeSpan.FromDays(1))
        {
            return $"{Math.Max(1, (int)diff.TotalHours)}h ago";
        }

        if (diff < TimeSpan.FromDays(7))
        {
            return $"{Math.Max(1, (int)diff.TotalDays)}d ago";
        }

        return value.ToString("MMM d");
    }

    private static string FormatStudyRecency(DateTime value, DateTime now)
    {
        var today = now.Date;
        if (value.Date == today)
        {
            return "Today";
        }

        if (value.Date == today.AddDays(-1))
        {
            return "Yesterday";
        }

        return FormatRelativeTime(value, now);
    }

    private static (string Action, string? Target) MapActivity(
        ActivityType type,
        string? deckTitle,
        int? streakDays,
        string? metadata)
    {
        var safeDeckTitle = string.IsNullOrWhiteSpace(deckTitle) ? "a deck" : deckTitle.Trim();

        return type switch
        {
            ActivityType.DeckCreated => ("created", safeDeckTitle),
            ActivityType.FirstDeckCreated => ("created their first deck", safeDeckTitle),
            ActivityType.DeckStudySessionCompleted => ("studied", safeDeckTitle),
            ActivityType.DeckMastered => ("mastered", safeDeckTitle),
            ActivityType.DayStreakReached => (
                streakDays is > 0 ? $"reached a {streakDays}-day streak in" : "reached a study streak in",
                "their studies"),
            ActivityType.FirstStudySessionCompleted => ("completed a first study session in", safeDeckTitle),
            _ => (string.IsNullOrWhiteSpace(metadata) ? "recorded activity in" : metadata.Trim(), safeDeckTitle)
        };
    }
}
