using Languag.io.Application.Users;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Languag.io.Infrastructure.Users;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new UserProfileDto(
                user.Id,
                user.ExternalId,
                user.Username,
                user.Name,
                user.Email,
                user.HasBeenOnboarded,
                user.DailyCardsGoal,
                user.AvatarColor,
                user.ProfileDescription,
                user.About,
                user.IsPublicProfile,
                user.CreatedAtUtc))
            .SingleOrDefaultAsync(ct);

        if (profile is null)
        {
            return null;
        }

        return profile with
        {
            RecentActivity = await ReadRecentActivityAsync(userId, ct),
            Stats = await ReadStatsAsync(userId, ct)
        };
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, Guid excludingUserId, CancellationToken ct = default)
    {
        return !await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id != excludingUserId && user.Username == username,
                ct);
    }

    public async Task<UpdateUserProfileResult> UpdateAsync(UpdateUserProfileCommand command, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(existingUser => existingUser.Id == command.UserId, ct);
        if (user is null)
        {
            return new UpdateUserProfileResult(UpdateUserProfileStatus.NotFound);
        }

        user.Username = command.Username;
        user.Name = command.Name;
        user.HasBeenOnboarded = command.HasBeenOnboarded;
        user.DailyCardsGoal = command.DailyCardsGoal;
        user.AvatarColor = command.AvatarColor;
        user.ProfileDescription = command.ProfileDescription;
        user.About = command.About;
        user.IsPublicProfile = command.IsPublicProfile;
        user.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUsernameConflict(ex))
        {
            return new UpdateUserProfileResult(
                UpdateUserProfileStatus.UsernameTaken,
                Error: "That username is already taken.");
        }

        return new UpdateUserProfileResult(
            UpdateUserProfileStatus.Updated,
            await MapToDtoAsync(user, ct));
    }

    private static bool IsUsernameConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(postgresException.ConstraintName, "IX_Users_Username", StringComparison.Ordinal);
    }

    private async Task<UserProfileDto> MapToDtoAsync(User user, CancellationToken ct)
    {
        return new UserProfileDto(
            user.Id,
            user.ExternalId,
            user.Username,
            user.Name,
            user.Email,
            user.HasBeenOnboarded,
            user.DailyCardsGoal,
            user.AvatarColor,
            user.ProfileDescription,
            user.About,
            user.IsPublicProfile,
            user.CreatedAtUtc,
            await ReadRecentActivityAsync(user.Id, ct),
            await ReadStatsAsync(user.Id, ct));
    }

    private async Task<UserProfileStatsDto> ReadStatsAsync(Guid userId, CancellationToken ct)
    {
        var decksCreated = await _dbContext.Decks
            .AsNoTracking()
            .CountAsync(deck => deck.OwnerId == userId, ct);

        var cardsStudied = await _dbContext.StudySessionResponses
            .AsNoTracking()
            .CountAsync(response => response.UserId == userId, ct);

        var masteredDecks = await _dbContext.StudySessions
            .AsNoTracking()
            .CountAsync(studySession => studySession.UserId == userId && studySession.PercentageCorrect == 100m, ct);

        return new UserProfileStatsDto(
            DecksCreated: decksCreated,
            CardsStudied: cardsStudied,
            MasteredDecks: masteredDecks,
            StudyStreakDays: 0);
    }

    private async Task<IReadOnlyList<UserProfileActivityDto>> ReadRecentActivityAsync(Guid userId, CancellationToken ct)
    {
        var activities = await _dbContext.ActivityLogs
            .AsNoTracking()
            .Where(activity => activity.UserId == userId)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .Take(10)
            .Select(activity => new
            {
                activity.Id,
                activity.Type,
                activity.StreakDays,
                activity.Metadata,
                activity.OccurredAtUtc,
                DeckTitle = activity.Deck != null ? activity.Deck.Title : null
            })
            .ToListAsync(ct);

        return activities
            .Select(activity => MapActivity(activity.Id, activity.Type, activity.DeckTitle, activity.StreakDays, activity.Metadata, activity.OccurredAtUtc))
            .ToArray();
    }

    private static UserProfileActivityDto MapActivity(
        Guid id,
        ActivityType type,
        string? deckTitle,
        int? streakDays,
        string? metadata,
        DateTime occurredAtUtc)
    {
        var safeDeckTitle = string.IsNullOrWhiteSpace(deckTitle) ? "a deck" : deckTitle.Trim();

        return type switch
        {
            ActivityType.DeckCreated => new UserProfileActivityDto(
                id,
                type.ToString(),
                $"Created {safeDeckTitle}",
                "Added a new deck to the library.",
                occurredAtUtc),
            ActivityType.FirstDeckCreated => new UserProfileActivityDto(
                id,
                type.ToString(),
                "Created first deck",
                $"Started learning with {safeDeckTitle}.",
                occurredAtUtc),
            ActivityType.DeckStudySessionCompleted => new UserProfileActivityDto(
                id,
                type.ToString(),
                $"Completed a study session for {safeDeckTitle}",
                "Finished reviewing cards in this deck.",
                occurredAtUtc),
            ActivityType.DeckMastered => new UserProfileActivityDto(
                id,
                type.ToString(),
                $"Mastered {safeDeckTitle}",
                "Completed a perfect study session for this deck.",
                occurredAtUtc),
            ActivityType.DayStreakReached => new UserProfileActivityDto(
                id,
                type.ToString(),
                streakDays is > 0 ? $"Reached a {streakDays}-day streak" : "Reached a study streak",
                "Stayed consistent with study sessions.",
                occurredAtUtc),
            ActivityType.FirstStudySessionCompleted => new UserProfileActivityDto(
                id,
                type.ToString(),
                "Completed first study session",
                $"Finished the first recorded session for {safeDeckTitle}.",
                occurredAtUtc),
            _ => new UserProfileActivityDto(
                id,
                type.ToString(),
                metadata ?? "Recorded activity",
                null,
                occurredAtUtc)
        };
    }
}
