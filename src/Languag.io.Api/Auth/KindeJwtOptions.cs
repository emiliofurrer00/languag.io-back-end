namespace Languag.io.Api.Auth;

public sealed class KindeJwtOptions
{
    public const string SectionName = "Authentication:Kinde";

    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
