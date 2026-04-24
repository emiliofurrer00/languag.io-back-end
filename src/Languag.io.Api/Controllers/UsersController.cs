using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Users;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserIdentityService _userIdentityService;
    private readonly IUserProfileService _userProfileService;
    private readonly IProfilePictureService _profilePictureService;

    public UsersController(
        IUserIdentityService userIdentityService,
        IUserProfileService userProfileService,
        IProfilePictureService profilePictureService)
    {
        _userIdentityService = userIdentityService;
        _userProfileService = userProfileService;
        _profilePictureService = profilePictureService;
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

    [AllowAnonymous]
    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> GetPublicProfileByUsername([FromRoute] string username, CancellationToken ct)
    {
        var profile = await _userProfileService.GetPublicByUsernameAsync(username, ct);
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
                request.AvatarColor,
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

    [EnableRateLimiting("profile-image-upload")]
    [HttpPost("me/profile-picture/upload-request")]
    public async Task<IActionResult> CreateProfilePictureUpload(
        [FromBody] CreateProfilePictureUploadRequest request,
        CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var result = await _profilePictureService.CreateUploadAsync(
            userId,
            request.ContentType,
            request.ContentLength,
            ct);

        if (result.Status == CreateProfilePictureUploadStatus.Invalid || result.Target is null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid profile picture upload",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(new CreateProfilePictureUploadResponse(
            result.Target.UploadUrl,
            result.Target.Fields,
            result.Target.ObjectKey,
            result.Target.PublicUrl,
            result.Target.ExpiresAtUtc,
            result.Target.MaxBytes));
    }

    [EnableRateLimiting("profile-image-upload")]
    [HttpPost("me/profile-picture/complete")]
    public async Task<IActionResult> CompleteProfilePictureUpload(
        [FromBody] CompleteProfilePictureUploadRequest request,
        CancellationToken ct)
    {
        var authenticatedUser = User.ToAuthenticatedUser();
        if (authenticatedUser is null)
        {
            return Unauthorized();
        }

        var userId = await _userIdentityService.GetOrCreateUserIdAsync(authenticatedUser, ct);
        var result = await _profilePictureService.CompleteUploadAsync(userId, request.ObjectKey, ct);

        return result.Status switch
        {
            CompleteProfilePictureUploadStatus.Updated => Ok(result.Profile),
            CompleteProfilePictureUploadStatus.NotFound => NotFound(),
            CompleteProfilePictureUploadStatus.Invalid => BadRequest(new ProblemDetails
            {
                Title = "Invalid profile picture upload",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
