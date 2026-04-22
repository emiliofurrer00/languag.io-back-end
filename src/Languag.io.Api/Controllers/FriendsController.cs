using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Friends;
using Languag.io.Application.Common;
using Languag.io.Application.Friends;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class FriendsController : ControllerBase
{
    private const int DefaultPageSize = 20;

    private readonly IFriendService _friendService;
    private readonly IUserIdentityService _userIdentityService;

    public FriendsController(
        IFriendService friendService,
        IUserIdentityService userIdentityService)
    {
        _friendService = friendService;
        _userIdentityService = userIdentityService;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendFriendRequest(
        [FromBody] SendFriendRequestRequest request,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.SendFriendRequestAsync(
            new SendFriendRequestCommand(request.TargetUserId),
            userId.Value,
            ct);

        return result.Status switch
        {
            SendFriendRequestStatus.Created => Created(
                $"/api/friends/requests/{result.RequestId}",
                new { requestId = result.RequestId }),
            SendFriendRequestStatus.Invalid => BadRequest(CreateProblemDetails("Invalid friend request", result.Error, StatusCodes.Status400BadRequest)),
            SendFriendRequestStatus.TargetUserNotFound => NotFound(),
            SendFriendRequestStatus.AlreadyFriends or
            SendFriendRequestStatus.PendingRequestAlreadyExists or
            SendFriendRequestStatus.ReversePendingRequestExists => Conflict(CreateProblemDetails("Friend request conflict", result.Error, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    public async Task<IActionResult> AcceptFriendRequest([FromRoute] Guid requestId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.AcceptFriendRequestAsync(
            new AcceptFriendRequestCommand(requestId),
            userId.Value,
            ct);

        return MapCommandResult(result);
    }

    [HttpPost("requests/{requestId:guid}/reject")]
    public async Task<IActionResult> RejectFriendRequest([FromRoute] Guid requestId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.RejectFriendRequestAsync(
            new RejectFriendRequestCommand(requestId),
            userId.Value,
            ct);

        return MapCommandResult(result);
    }

    [HttpPost("requests/{requestId:guid}/cancel")]
    public async Task<IActionResult> CancelFriendRequest([FromRoute] Guid requestId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.CancelFriendRequestAsync(
            new CancelFriendRequestCommand(requestId),
            userId.Value,
            ct);

        return MapCommandResult(result);
    }

    [HttpGet("requests/incoming")]
    public async Task<IActionResult> GetIncomingRequests(
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

        var page = await _friendService.GetIncomingFriendRequestsAsync(
            new GetIncomingFriendRequestsQuery(parsedCursor, pageSize),
            userId.Value,
            ct);

        return Ok(page);
    }

    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> GetOutgoingRequests(
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

        var page = await _friendService.GetOutgoingFriendRequestsAsync(
            new GetOutgoingFriendRequestsQuery(parsedCursor, pageSize),
            userId.Value,
            ct);

        return Ok(page);
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends(
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

        var page = await _friendService.GetFriendsAsync(
            new GetFriendsQuery(parsedCursor, pageSize),
            userId.Value,
            ct);

        return Ok(page);
    }

    [HttpGet("status/{otherUserId:guid}")]
    public async Task<IActionResult> GetFriendshipStatus([FromRoute] Guid otherUserId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.GetFriendshipStatusAsync(
            new GetFriendshipStatusQuery(otherUserId),
            userId.Value,
            ct);

        return result.Status switch
        {
            GetFriendshipStatusResultStatus.Success => Ok(result.FriendshipStatus),
            GetFriendshipStatusResultStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpDelete("{friendUserId:guid}")]
    public async Task<IActionResult> RemoveFriend([FromRoute] Guid friendUserId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _friendService.RemoveFriendAsync(
            new RemoveFriendCommand(friendUserId),
            userId.Value,
            ct);

        return result.Status switch
        {
            RemoveFriendStatus.Success => Ok(),
            RemoveFriendStatus.NotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult MapCommandResult(FriendRequestCommandResult result)
    {
        return result.Status switch
        {
            FriendRequestCommandStatus.Success => Ok(),
            FriendRequestCommandStatus.NotFound => NotFound(),
            FriendRequestCommandStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden),
            FriendRequestCommandStatus.Conflict => Conflict(CreateProblemDetails("Friend request conflict", result.Error, StatusCodes.Status409Conflict)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
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
