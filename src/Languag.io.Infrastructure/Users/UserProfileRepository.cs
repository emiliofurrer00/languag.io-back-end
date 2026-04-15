using Languag.io.Application.Users;
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
        return await _dbContext.Users
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
                user.IsPublicProfile))
            .SingleOrDefaultAsync(ct);
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
            MapToDto(user));
    }

    private static bool IsUsernameConflict(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(postgresException.ConstraintName, "IX_Users_Username", StringComparison.Ordinal);
    }

    private static UserProfileDto MapToDto(Domain.Entities.User user)
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
            user.IsPublicProfile);
    }
}
