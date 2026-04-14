using System.Text.Json;
using Languag.io.Api.Contracts.Webhooks;
using Languag.io.Api.Webhooks;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/decks/users")]
public sealed class UsersWebhookController : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IKindeWebhookDecoder _webhookDecoder;
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILogger<UsersWebhookController> _logger;

    public UsersWebhookController(
        IKindeWebhookDecoder webhookDecoder,
        IUserIdentityService userIdentityService,
        ILogger<UsersWebhookController> logger)
    {
        _webhookDecoder = webhookDecoder;
        _userIdentityService = userIdentityService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var requestId = Request.Headers["webhook-id"].ToString();
        var mediaType = NormalizeMediaType(Request.ContentType);
        var rawBody = await ReadRequestBodyAsync(Request, ct);

        if (!IsSupportedMediaType(mediaType, rawBody))
        {
            _logger.LogWarning(
                "Rejected Kinde webhook {RequestId} with unsupported media type {MediaType}.",
                requestId,
                Request.ContentType);
            return StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            _logger.LogWarning("Rejected Kinde webhook {RequestId} because the request body was empty.", requestId);
            return BadRequest();
        }

        var payload = await ParsePayloadAsync(rawBody, mediaType, ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Type))
        {
            _logger.LogWarning(
                "Rejected Kinde webhook {RequestId} because the payload could not be decoded. ContentType={ContentType}",
                requestId,
                Request.ContentType);

            return BadRequest();
        }

        await HandleEventAsync(payload, requestId, ct);
        return Ok();
    }

    private async Task<WebhookEnvelope?> ParsePayloadAsync(string rawBody, string? mediaType, CancellationToken ct)
    {
        if (mediaType == "application/json")
        {
            try
            {
                return JsonSerializer.Deserialize<WebhookEnvelope>(rawBody, SerializerOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Kinde webhook JSON payload.");
                return null;
            }
        }

        if (mediaType is null || mediaType == "application/jwt" || mediaType == "text/plain" || LooksLikeJwt(rawBody))
        {
            var decodeResult = await _webhookDecoder.DecodeAsync(rawBody, ct);
            if (!decodeResult.IsValid)
            {
                _logger.LogWarning("Failed to validate Kinde webhook JWT. Reason={Reason}", decodeResult.Error);
                return null;
            }

            return decodeResult.Payload;
        }

        try
        {
            return JsonSerializer.Deserialize<WebhookEnvelope>(rawBody, SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse fallback Kinde webhook payload.");
            return null;
        }
    }

    private async Task HandleEventAsync(WebhookEnvelope payload, string? requestId, CancellationToken ct)
    {
        switch (payload.Type)
        {
            case "user.created":
            case "user.updated":
            {
                var user = payload.Data?.User;
                if (string.IsNullOrWhiteSpace(user?.Id))
                {
                    _logger.LogWarning(
                        "Kinde webhook {RequestId} had event type {EventType} but no user id.",
                        requestId,
                        payload.Type);
                    return;
                }

                var authenticatedUser = new AuthenticatedUser(
                    user.Id,
                    user.Email,
                    BuildDisplayName(user));

                await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);

                _logger.LogInformation(
                    "Processed Kinde webhook {RequestId}. EventType={EventType}, EventId={EventId}, UserId={UserId}",
                    requestId,
                    payload.Type,
                    payload.EventId,
                    user.Id);
                return;
            }

            case "user.deleted":
                _logger.LogInformation(
                    "Received Kinde webhook {RequestId} for {EventType}. No destructive action is taken yet. EventId={EventId}",
                    requestId,
                    payload.Type,
                    payload.EventId);
                return;

            default:
                _logger.LogInformation(
                    "Ignoring unsupported Kinde webhook {RequestId}. EventType={EventType}, EventId={EventId}",
                    requestId,
                    payload.Type,
                    payload.EventId);
                return;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, CancellationToken ct)
    {
        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        ct.ThrowIfCancellationRequested();
        var rawBody = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return rawBody;
    }

    private static string? NormalizeMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        return contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0].ToLowerInvariant();
    }

    private static bool IsSupportedMediaType(string? mediaType, string rawBody)
    {
        return mediaType is null
            || mediaType == "application/jwt"
            || mediaType == "text/plain"
            || mediaType == "application/json"
            || LooksLikeJwt(rawBody);
    }

    private static bool LooksLikeJwt(string rawBody)
    {
        return rawBody.Count(c => c == '.') == 2;
    }

    private static string? BuildDisplayName(WebhookUser user)
    {
        var parts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            return user.Username.Trim();
        }

        return null;
    }
}
