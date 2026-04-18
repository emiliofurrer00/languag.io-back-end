namespace Languag.io.Application.Users;

public sealed record UserProfileActivityDto(
    Guid Id,
    string Type,
    string Title,
    string? Description,
    DateTime OccurredAt);
