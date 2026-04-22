using Languag.io.Api.Auth;
using Languag.io.Application.Feed;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;
    private readonly IUserIdentityService _userIdentityService;

    public FeedController(
        IFeedService feedService,
        IUserIdentityService userIdentityService)
    {
        _feedService = feedService;
        _userIdentityService = userIdentityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFeed(CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var feed = await _feedService.GetFeedAsync(userId, ct);
        if (feed is null)
        {
            return NotFound();
        }

        return Ok(feed);
    }
}
