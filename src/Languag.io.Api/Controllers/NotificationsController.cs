using Languag.io.Api.Auth;
using Languag.io.Application.Common;
using Languag.io.Application.Notifications;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private const int DefaultPageSize = 20;

    private readonly INotificationService _notificationService;
    private readonly IUserIdentityService _userIdentityService;

    public NotificationsController(
        INotificationService notificationService,
        IUserIdentityService userIdentityService)
    {
        _notificationService = notificationService;
        _userIdentityService = userIdentityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!TryBuildCursor(cursor, out var parsedCursor, out var errorResult))
        {
            return errorResult!;
        }

        var page = await _notificationService.GetNotificationsAsync(
            new GetNotificationsQuery(parsedCursor, pageSize),
            userId.Value,
            ct);

        return Ok(page);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadNotificationCount(CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(
            new GetUnreadNotificationCountQuery(),
            userId.Value,
            ct);

        return Ok(unreadCount);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead([FromRoute] Guid notificationId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkNotificationReadAsync(
            new MarkNotificationReadCommand(notificationId),
            userId.Value,
            ct);

        return result.Status switch
        {
            MarkNotificationReadStatus.Success => Ok(),
            MarkNotificationReadStatus.NotFound => NotFound(),
            MarkNotificationReadStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var updatedCount = await _notificationService.MarkAllNotificationsReadAsync(
            new MarkAllNotificationsReadCommand(),
            userId.Value,
            ct);

        return Ok(new { updatedCount });
    }

    private async Task<Guid?> GetCurrentUserIdAsync(CancellationToken ct)
    {
        var currentUser = User.ToAuthenticatedUser();
        if (currentUser is null)
        {
            return null;
        }

        return await _userIdentityService.GetOrCreateUserIdAsync(currentUser, ct);
    }

    private static ProblemDetails CreateProblemDetails(string title, string? detail, int statusCode)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode
        };
    }

    private static bool TryBuildCursor(string? cursor, out TimelineCursor? parsedCursor, out IActionResult? errorResult)
    {
        parsedCursor = null;
        errorResult = null;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        if (TimelineCursor.TryDecode(cursor, out var decodedCursor))
        {
            parsedCursor = decodedCursor;
            return true;
        }

        errorResult = new BadRequestObjectResult(CreateProblemDetails(
            "Invalid pagination cursor",
            "The supplied cursor is not valid.",
            StatusCodes.Status400BadRequest));
        return false;
    }
}
