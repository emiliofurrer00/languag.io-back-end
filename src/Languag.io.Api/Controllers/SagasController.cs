using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Sagas;
using Languag.io.Application.Sagas;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SagasController : ControllerBase
{
    private readonly ISagaService _sagaService;
    private readonly IUserIdentityService _userIdentityService;

    public SagasController(
        ISagaService sagaService,
        IUserIdentityService userIdentityService)
    {
        _sagaService = sagaService;
        _userIdentityService = userIdentityService;
    }

    // GET: api/sagas
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetVisibleSagas(CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var sagas = await _sagaService.GetVisibleSagasAsync(userId.Value, ct);
        return Ok(sagas);
    }

    // GET: api/sagas/public
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicSagas(CancellationToken ct)
    {
        var sagas = await _sagaService.GetPublicSagasAsync(ct);
        return Ok(sagas);
    }

    // GET: api/sagas/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSagaById([FromRoute] Guid id, CancellationToken ct)
    {
        Guid? currentUserId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            currentUserId = await GetCurrentUserIdAsync(ct);
            if (currentUserId is null)
            {
                return Unauthorized();
            }
        }

        var saga = await _sagaService.GetSagaByIdAsync(id, currentUserId, ct);
        if (saga is null)
        {
            return NotFound();
        }

        return Ok(saga);
    }

    // POST: api/sagas
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateSaga(
        [FromBody] CreateSagaRequest request,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = new CreateSagaCommand(
            request.Title,
            request.Description,
            request.Category,
            request.Color,
            request.Visibility,
            request.Chapters.Select(chapter => new CreateSagaChapterCommand(
                chapter.Title,
                chapter.Description,
                chapter.Order,
                chapter.Lessons.Select(lesson => new CreateSagaLessonCommand(
                    lesson.DeckId,
                    lesson.Title,
                    lesson.Description,
                    lesson.Order)).ToArray())).ToArray());

        var result = await _sagaService.CreateSagaAsync(command, userId.Value, ct);

        return result.Status switch
        {
            CreateSagaStatus.Created => CreatedAtAction(
                nameof(GetSagaById),
                new { id = result.SagaId },
                new { sagaId = result.SagaId }),
            CreateSagaStatus.DeckNotFound => BadRequest(new { message = result.Error }),
            CreateSagaStatus.Invalid => BadRequest(new { message = result.Error }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    // POST: api/sagas/{id}/lessons/{lessonId}/complete
    [Authorize]
    [HttpPost("{id:guid}/lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(
        [FromRoute] Guid id,
        [FromRoute] Guid lessonId,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _sagaService.CompleteLessonAsync(id, lessonId, userId.Value, ct);

        return result.Status switch
        {
            CompleteSagaLessonStatus.Completed => Ok(result.Progress),
            CompleteSagaLessonStatus.SagaNotFound => NotFound(),
            CompleteSagaLessonStatus.LessonNotFound => NotFound(),
            CompleteSagaLessonStatus.Invalid => BadRequest(new { message = result.Error }),
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
}
