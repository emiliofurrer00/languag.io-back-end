using System.Security.Claims;
using Languag.io.Application.Users;

namespace Languag.io.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static AuthenticatedUser? ToAuthenticatedUser(this ClaimsPrincipal principal)
    {
        var externalId = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var name = principal.FindFirstValue("name")
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? BuildName(principal);

        return new AuthenticatedUser(
            externalId,
            principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email),
            name);
    }

    private static string? BuildName(ClaimsPrincipal principal)
    {
        var givenName = principal.FindFirstValue("given_name") ?? principal.FindFirstValue(ClaimTypes.GivenName);
        var familyName = principal.FindFirstValue("family_name") ?? principal.FindFirstValue(ClaimTypes.Surname);

        var fullName = string.Join(' ', new[] { givenName, familyName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }

    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }
}
