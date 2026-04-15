using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Users;
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

    [HttpGet("username-availability")]
    public async Task<IActionResult> GetUsernameAvailability([FromQuery] string? username, CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        var normalizedUsername = username?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            ModelState.AddModelError(nameof(username), "Username is required.");
            return ValidationProblem(ModelState);
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var isAvailable = await _userProfileService.IsUsernameAvailableAsync(normalizedUsername, userId, ct);

        return Ok(new UsernameAvailabilityResponse(normalizedUsername, isAvailable));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserProfileRequest request, CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        if (request.HasBeenOnboarded && string.IsNullOrWhiteSpace(request.Username))
        {
            ModelState.AddModelError(nameof(request.Username), "Username is required when onboarding is completed.");
            return ValidationProblem(ModelState);
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var result = await _userProfileService.UpdateAsync(
            new UpdateUserProfileCommand(
                userId,
                request.Username,
                request.Name,
                request.HasBeenOnboarded,
                request.DailyCardsGoal,
                request.ProfileDescription,
                request.About,
                request.IsPublicProfile),
            ct);

        return result.Status switch
        {
            UpdateUserProfileStatus.Updated => Ok(result.Profile),
            UpdateUserProfileStatus.UsernameTaken => Conflict(new ProblemDetails
            {
                Title = "Username unavailable",
                Detail = result.Error,
                Status = StatusCodes.Status409Conflict
            }),
            UpdateUserProfileStatus.NotFound => NotFound(),
            UpdateUserProfileStatus.Invalid => BadRequest(new ProblemDetails
            {
                Title = "Invalid profile update",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
