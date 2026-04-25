using System.Text;
using Languag.io.Api.Contracts.Webhooks;
using Languag.io.Api.Webhooks;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Languag.io.Api.Controllers;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/decks/users")]
public sealed class UsersWebhookController : ControllerBase
{
    private const int MaxWebhookBodyBytes = 64 * 1024;
    private static readonly TimeSpan MaxWebhookAge = TimeSpan.FromMinutes(10);

    private readonly IKindeWebhookDecoder _webhookDecoder;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IMemoryCache _webhookReplayCache;
    private readonly ILogger<UsersWebhookController> _logger;

    public UsersWebhookController(
        IKindeWebhookDecoder webhookDecoder,
        IUserIdentityService userIdentityService,
        IMemoryCache webhookReplayCache,
        ILogger<UsersWebhookController> logger)
    {
        _webhookDecoder = webhookDecoder;
        _userIdentityService = userIdentityService;
        _webhookReplayCache = webhookReplayCache;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MaxWebhookBodyBytes)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var requestId = Request.Headers["webhook-id"].ToString();
        var mediaType = NormalizeMediaType(Request.ContentType);

        if (Request.ContentLength is > MaxWebhookBodyBytes)
        {
            _logger.LogWarning(
                "Rejected Kinde webhook {RequestId} because the request body exceeded {MaxWebhookBodyBytes} bytes.",
                requestId,
                MaxWebhookBodyBytes);
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

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

        if (Encoding.UTF8.GetByteCount(rawBody) > MaxWebhookBodyBytes)
        {
            _logger.LogWarning(
                "Rejected Kinde webhook {RequestId} because the decoded request body exceeded {MaxWebhookBodyBytes} bytes.",
                requestId,
                MaxWebhookBodyBytes);
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
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

        if (!IsFreshWebhook(payload))
        {
            _logger.LogWarning(
                "Rejected Kinde webhook {RequestId} because its timestamp was missing or outside the allowed window. EventId={EventId}",
                requestId,
                payload.EventId);
            return BadRequest();
        }

        var idempotencyKey = BuildIdempotencyKey(requestId, payload.EventId);
        if (idempotencyKey is not null && _webhookReplayCache.TryGetValue(idempotencyKey, out _))
        {
            _logger.LogInformation(
                "Ignored duplicate Kinde webhook {RequestId}. EventId={EventId}",
                requestId,
                payload.EventId);
            return Ok();
        }

        await HandleEventAsync(payload, requestId, ct);

        if (idempotencyKey is not null)
        {
            _webhookReplayCache.Set(idempotencyKey, true, MaxWebhookAge);
        }

        return Ok();
    }

    private async Task<WebhookEnvelope?> ParsePayloadAsync(string rawBody, string? mediaType, CancellationToken ct)
    {
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

        return null;
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
            || LooksLikeJwt(rawBody);
    }

    private static bool LooksLikeJwt(string rawBody)
    {
        return rawBody.Count(c => c == '.') == 2;
    }

    private static bool IsFreshWebhook(WebhookEnvelope payload)
    {
        var timestamp = payload.EventTimestamp ?? payload.Timestamp;
        if (timestamp is null)
        {
            return false;
        }

        return (DateTimeOffset.UtcNow - timestamp.Value.ToUniversalTime()).Duration() <= MaxWebhookAge;
    }

    private static string? BuildIdempotencyKey(string? requestId, string? eventId)
    {
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            return $"kinde-webhook-request:{requestId.Trim()}";
        }

        return string.IsNullOrWhiteSpace(eventId)
            ? null
            : $"kinde-webhook-event:{eventId.Trim()}";
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
