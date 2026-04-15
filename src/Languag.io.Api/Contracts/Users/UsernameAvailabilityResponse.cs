namespace Languag.io.Api.Contracts.Users;

public sealed record UsernameAvailabilityResponse(
    string Username,
    bool IsAvailable);
