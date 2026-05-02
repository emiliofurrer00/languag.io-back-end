using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Ai;
using Languag.io.Application.AiDeckGeneration;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/deck-generations")]
public class AiDeckGenerationsController : ControllerBase
{
    private readonly IAiDeckGenerationService _aiDeckGenerationService;
    private readonly IUserIdentityService _userIdentityService;

    public AiDeckGenerationsController(
        IAiDeckGenerationService aiDeckGenerationService,
        IUserIdentityService userIdentityService)
    {
        _aiDeckGenerationService = aiDeckGenerationService;
        _userIdentityService = userIdentityService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAiDeckGenerationRequest request,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var jobId = await _aiDeckGenerationService.CreateDeckGenerationJobAsync(
                new CreateAiDeckGenerationJobCommand(
                    request.Prompt,
                    request.TargetLanguage,
                    request.NativeLanguage,
                    request.Difficulty,
                    request.CardCount),
                userId.Value,
                ct);

            return AcceptedAtAction(nameof(GetById), new { jobId }, new { jobId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var job = await _aiDeckGenerationService.GetDeckGenerationJobAsync(jobId, userId.Value, ct);
        return job is null ? NotFound() : Ok(job);
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
}
