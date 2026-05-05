using System.Text;
using System.Text.Json;
using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Webhooks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Languag.io.Api.Webhooks;

public sealed class KindeWebhookDecoder : IKindeWebhookDecoder
{
    private static readonly HashSet<string> AllowedSigningAlgorithms = [SecurityAlgorithms.RsaSha256];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly string _authority;
    private readonly string _audience;
    private readonly ILogger<KindeWebhookDecoder> _logger;
    public KindeWebhookDecoder(IOptions<KindeJwtOptions> kindeOptions, ILogger<KindeWebhookDecoder> logger)
    {
        var authority = kindeOptions.Value.Authority?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException("Authentication:Kinde:Authority must be configured to decode Kinde webhooks.");
        }

        _authority = authority;
        _audience = kindeOptions.Value.Audience?.Trim() ?? string.Empty;
        _logger = logger;
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{authority}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever
            {
                RequireHttps = authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            });
    }

    public async Task<KindeWebhookDecodeResult> DecodeAsync(string rawJwt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawJwt))
        {
            return Invalid("Webhook body was empty.");
        }

        var jwt = rawJwt.Trim();
        if (!_tokenHandler.CanReadToken(jwt))
        {
            return Invalid("Webhook JWT could not be read.");
        }

        var token = _tokenHandler.ReadJwtToken(jwt);
        if (!AllowedSigningAlgorithms.Contains(token.Header.Alg))
        {
            return Invalid("Webhook JWT used an unsupported signing algorithm.");
        }

        var configuration = await LoadConfigurationAsync(ct);
        if (configuration is null)
        {
            return Invalid("Unable to load Kinde signing keys.");
        }

        var validation = await _tokenHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            // Kinde webhook JWTs are documented as signed event envelopes, not API/user access tokens.
            // Some webhook tokens are missing standard issuer/audience/lifetime claims, so the
            // mandatory authenticity check is the Kinde JWKS signature; any present registered
            // claims are checked below.
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            RequireExpirationTime = false,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAlgorithms = [.. AllowedSigningAlgorithms]
        });

        if (!validation.IsValid)
        {
            _logger.LogWarning(validation.Exception, "Rejected Kinde webhook JWT.");
            return Invalid("Webhook JWT validation failed.");
        }

        var registeredClaimValidationError = ValidateRegisteredClaims(token);
        if (registeredClaimValidationError is not null)
        {
            return Invalid(registeredClaimValidationError);
        }

        try
        {
            var payloadJson = DecodePayloadJson(jwt);
            var payload = JsonSerializer.Deserialize<WebhookEnvelope>(payloadJson, SerializerOptions);
            return payload is null
                ? Invalid("Webhook payload could not be parsed.")
                : new KindeWebhookDecodeResult(true, payload, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kinde webhook JWT was valid but its payload could not be parsed.");
            return Invalid("Webhook payload could not be decoded.");
        }
    }

    private string? ValidateRegisteredClaims(JwtSecurityToken token)
    {
        if (!string.IsNullOrWhiteSpace(token.Issuer) &&
            !string.Equals(token.Issuer.TrimEnd('/'), _authority, StringComparison.Ordinal))
        {
            return "Webhook JWT issuer did not match the configured Kinde authority.";
        }

        if (!string.IsNullOrWhiteSpace(_audience) &&
            token.Audiences.Any() &&
            !token.Audiences.Contains(_audience, StringComparer.Ordinal))
        {
            return "Webhook JWT audience did not match the configured Kinde audience.";
        }

        if (token.ValidTo != DateTime.MinValue && token.ValidTo <= DateTime.UtcNow)
        {
            return "Webhook JWT was expired.";
        }

        return null;
    }

    private async Task<OpenIdConnectConfiguration?> LoadConfigurationAsync(CancellationToken ct)
    {
        try
        {
            return await _configurationManager.GetConfigurationAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Kinde OpenID configuration for webhook verification.");
            return null;
        }
    }

    private static string DecodePayloadJson(string jwt)
    {
        var segments = jwt.Split('.');
        if (segments.Length != 3)
        {
            throw new InvalidOperationException("Webhook JWT did not contain three segments.");
        }

        var payloadBytes = Base64UrlEncoder.DecodeBytes(segments[1]);
        return Encoding.UTF8.GetString(payloadBytes);
    }

    private static KindeWebhookDecodeResult Invalid(string error) => new(false, null, error);
}
