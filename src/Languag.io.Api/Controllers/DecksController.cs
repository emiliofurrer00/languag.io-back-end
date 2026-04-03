using Languag.io.Api.Auth;
using Languag.io.Api.Contracts.Decks;
using Languag.io.Api.Contracts.Webhooks;
using Languag.io.Application.Decks;
using Languag.io.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Languag.io.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;
    private readonly IUserIdentityService _userIdentityService;

    public DecksController(IDeckService deckService, IUserIdentityService userIdentityService)
    {
        _deckService = deckService;
        _userIdentityService = userIdentityService;
    }

    // GET: api/decks
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetVisibleDecks(CancellationToken ct)
    {
        var currentUserId = await GetCurrentUserIdAsync(ct);
        if (currentUserId is null)
        {
            return Unauthorized();
        }

        var decks = await _deckService.GetVisibleDecksAsync(currentUserId.Value, ct);
        return Ok(decks);
    }

    // GET: api/decks/public
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicDecks(CancellationToken ct)
    {
        var decks = await _deckService.GetPublicDecksAsync(ct);
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

    // Temp test webhook endpoint
    // TODO: Refactor and move out of this controller

    // POST: api/decks/users
    [HttpPost("users")]
    public IActionResult UserEventWebhook(WebhookEnvelope envelope)
    {
        var evt = envelope.Data;

        switch (evt.Type)
        {
            case "user.created":
            {
                var user = evt.Payload;

                var primaryEmail = user.EmailAddresses
                    .OrderByDescending(e => e.Id == user.PrimaryEmailAddressId)
                    .Select(e => e.Email)
                    .FirstOrDefault();

                Console.Write(
                    "user.created => id={UserId}, email={Email}, name={First} {Last}, ip={Ip}",
                    user.Id,
                    primaryEmail,
                    user.FirstName,
                    user.LastName,
                    evt.EventAttributes?.HttpRequest?.ClientIp
                );

                // TODO: do your real work here (save to DB, enqueue job, etc.)
                break;
            }

            default:
                Console.Write("Unhandled webhook type: {Type}", evt.Type);
                break;
        }

        return Ok();
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
