using Languag.io.Application.Users;
using Languag.io.Domain.Entities;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Users;

public class UserIdentityService : IUserIdentityService
{
    private readonly AppDbContext _dbContext;

    public UserIdentityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> GetOrCreateUserIdAsync(AuthenticatedUser user, CancellationToken ct = default)
    {
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.ExternalId == user.ExternalId, ct);

        if (existingUser is not null)
        {
            var isUpdated = false;

            if (existingUser.Email != user.Email)
            {
                existingUser.Email = user.Email;
                isUpdated = true;
            }

            if (existingUser.Name != user.Name)
            {
                existingUser.Name = user.Name;
                isUpdated = true;
            }

            if (isUpdated)
            {
                await _dbContext.SaveChangesAsync(ct);
            }

            return existingUser.Id;
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = user.ExternalId,
            Email = user.Email,
            Name = user.Name
        };

        _dbContext.Users.Add(newUser);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
            return newUser.Id;
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(newUser).State = EntityState.Detached;

            existingUser = await _dbContext.Users
                .SingleOrDefaultAsync(u => u.ExternalId == user.ExternalId, ct);

            if (existingUser is not null)
            {
                return existingUser.Id;
            }

            throw;
        }
    }
}
