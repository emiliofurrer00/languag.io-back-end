namespace Languag.io.Application.Users;

public sealed record AuthenticatedUser(
    string ExternalId,
    string? Email,
    string? Name);
