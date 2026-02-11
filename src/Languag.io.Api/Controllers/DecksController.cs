using Languag.io.Application.Decks;
using Microsoft.AspNetCore.Mvc;
using Languag.io.Api.Contracts.Decks;

namespace Languag.io.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;
    public DecksController(IDeckService deckService)
    {
        _deckService = deckService;
    }

    // GET: api/decks/public
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicDecks(CancellationToken ct)
    {
        var decks = await _deckService.GetPublicDecksAsync(ct);
        return Ok(decks);
    }

    // POST: api/decks
    [HttpPost()]
    public async Task<IActionResult> CreateNewDeck([FromBody] CreateDeckRequest request, CancellationToken ct)
    {
        // Mock Id, replace with authenticated user id later
        var userId = Guid.NewGuid();

        var command = new CreateDeckCommand(
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Color: request.Color,
            Visibility: request.Visibility
        );

        var deckId = await _deckService.CreateDeckAsync(command, userId, ct);

        return CreatedAtAction(nameof(GetPublicDecks), new { id = deckId }, deckId);
    }

    // PUT: api/decks/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDeck([FromRoute] string id, [FromBody] UpdateDeckRequest request, CancellationToken ct)
    {
        // Mock Id, replace with authenticated user id later
        var userId = Guid.NewGuid();
        var command = new UpdateDeckCommand(
            Id: Guid.Parse(id),
            Title: request.Title,
            Description: request.Description,
            Category: request.Category,
            Color: request.Color,
            Visibility: request.Visibility
        );
        var result = await _deckService.UpdateDeckAsync(command, userId, ct);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // GET: api/decks/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDeckById([FromRoute] string id, CancellationToken ct)
    {
        var deck = await _deckService.GetDeckByIdAsync(Guid.Parse(id), ct);
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
    public async Task<IActionResult> UserEventWebhook([FromBody] object data, [FromBody] string type) {
        Console.WriteLine("Object data: ");
        Console.WriteLine(data);
        Console.WriteLine("Event type: " + type);

        return Ok();
    }

}
