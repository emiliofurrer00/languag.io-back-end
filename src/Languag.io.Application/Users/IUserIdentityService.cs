namespace Languag.io.Application.Users;

public interface IUserIdentityService
{
    Task<Guid> GetOrCreateUserIdAsync(AuthenticatedUser user, CancellationToken ct = default);
}
