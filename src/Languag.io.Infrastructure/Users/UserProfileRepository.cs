using Languag.io.Application.Users;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                user.ProfileDescription,
                user.About,
                user.IsPublicProfile))
            .SingleOrDefaultAsync(ct);
    }
}
