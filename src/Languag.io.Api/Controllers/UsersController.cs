using Languag.io.Api.Auth;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserIdentityService userIdentityService, IUserProfileService userProfileService)
    {
        _userIdentityService = userIdentityService;
        _userProfileService = userProfileService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var profile = await _userProfileService.GetByIdAsync(userId, ct);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }
}
