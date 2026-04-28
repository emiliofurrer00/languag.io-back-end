using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Api.Contracts.StudySessions;
using Languag.io.Application.Decks;
using Languag.io.Application.StudySessions;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;
    private readonly IStudySessionService _studySessionService;
    private readonly IUserIdentityService _userIdentityService;

    public DecksController(
        IDeckService deckService,
        IStudySessionService studySessionService,
        IUserIdentityService userIdentityService)
    {
        _deckService = deckService;
        _studySessionService = studySessionService;
        _userIdentityService = userIdentityService;
    }

    // GET: api/decks
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetVisibleDecks([FromQuery] DeckListQuery query, CancellationToken ct)
    {
        var currentUserId = await GetCurrentUserIdAsync(ct);
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var decks = await _deckService.GetVisibleDecksAsync(currentUserId.Value, query, ct);
        return Ok(decks);
    }

    // GET: api/decks/public
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicDecks([FromQuery] DeckListQuery query, CancellationToken ct)
    {
        var decks = await _deckService.GetPublicDecksAsync(query, ct);
        return Ok(decks);
    }

    // POST: api/decks
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateNewDeck([FromBody] CreateDeckRequest request, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = new CreateDeckCommand(
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Color: request.Color,
            Visibility: request.Visibility,
            Cards: request.Cards
        );

        var deckId = await _deckService.CreateDeckAsync(command, userId.Value, ct);

        return CreatedAtAction(nameof(GetDeckById), new { id = deckId }, deckId);
    }

    // PUT: api/decks/{id}
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDeck([FromRoute] Guid id, [FromBody] UpdateDeckRequest request, CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = new UpdateDeckCommand(
            Id: id,
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Color: request.Color,
            Visibility: request.Visibility,
            Cards: request.Cards

        );

        var result = await _deckService.UpdateDeckAsync(command, userId.Value, ct);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/decks/{id}/study-sessions
    [Authorize]
    [HttpPost("{id:guid}/study-sessions")]
    public async Task<IActionResult> SubmitStudySession(
        [FromRoute] Guid id,
        [FromBody] SubmitStudySessionRequest request,
        CancellationToken ct)
    {
        var userId = await GetCurrentUserIdAsync(ct);
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = new SubmitStudySessionCommand(
            id,
            request.PercentageCorrect,
            request.Responses
                .Select(response => new SubmitStudySessionResponseCommand(
                    response.CardId,
                    response.WasCorrect))
                .ToArray());

        var result = await _studySessionService.SubmitAsync(command, userId.Value, ct);

        return result.Status switch
        {
            SubmitStudySessionStatus.Created => Ok(new { studySessionId = result.StudySessionId }),
            SubmitStudySessionStatus.DeckNotFound => NotFound(),
            SubmitStudySessionStatus.Invalid => BadRequest(new { message = result.Error }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    // GET: api/decks/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeckById([FromRoute] Guid id, CancellationToken ct)
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

        var deck = await _deckService.GetDeckByIdAsync(id, currentUserId, ct);
        if (deck == null)
        {
            return NotFound();
        }

        return Ok(deck);
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
