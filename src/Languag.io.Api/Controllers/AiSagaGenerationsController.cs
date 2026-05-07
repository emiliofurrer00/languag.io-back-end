using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Ai;
using Languag.io.Application.AiSagaGeneration;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ai/saga-generations")]
public class AiSagaGenerationsController : ControllerBase
{
    private readonly IAiSagaGenerationService _aiSagaGenerationService;
    private readonly IUserIdentityService _userIdentityService;

    public AiSagaGenerationsController(
        IAiSagaGenerationService aiSagaGenerationService,
        IUserIdentityService userIdentityService)
    {
        _aiSagaGenerationService = aiSagaGenerationService;
        _userIdentityService = userIdentityService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAiSagaGenerationRequest request,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _aiSagaGenerationService.CreateSagaGenerationJobAsync(
            new CreateAiSagaGenerationJobCommand(
                request.Prompt,
                request.TargetLanguage,
                request.NativeLanguage,
                request.Difficulty,
                request.DeckCount,
                request.CardsPerDeck,
                request.IncludeAudio,
                request.MultiChoiceCountPerDeck),
            userId.Value,
            ct);

        return result.Status switch
        {
            CreateAiSagaGenerationStatus.Created => AcceptedAtAction(
                nameof(GetById),
                new { jobId = result.JobId },
                new
                {
                    jobId = result.JobId,
                    nextAllowedAtUtc = result.NextAllowedAtUtc
                }),
            CreateAiSagaGenerationStatus.Invalid => BadRequest(new { message = result.Error }),
            CreateAiSagaGenerationStatus.WeeklyLimitExceeded => WeeklyLimitExceeded(result),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid jobId, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var job = await _aiSagaGenerationService.GetSagaGenerationJobAsync(jobId, userId.Value, ct);
        return job is null ? NotFound() : Ok(job);
    }

    private IActionResult WeeklyLimitExceeded(CreateAiSagaGenerationResult result)
    {
        if (result.NextAllowedAtUtc is DateTime nextAllowedAtUtc)
        {
            var retryAfterSeconds = Math.Max(
                0,
                (int)Math.Ceiling((nextAllowedAtUtc - DateTime.UtcNow).TotalSeconds));

            Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        }

        return StatusCode(
            StatusCodes.Status429TooManyRequests,
            new
            {
                message = result.Error,
                nextAllowedAtUtc = result.NextAllowedAtUtc
            });
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
