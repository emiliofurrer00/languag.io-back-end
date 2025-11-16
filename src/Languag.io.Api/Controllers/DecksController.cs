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
            LanguageCode: request.LanguageCode,
            Visibility: request.Visibility
        );

        var deckId = await _deckService.CreateDeckAsync(command, userId, ct);

        return CreatedAtAction(nameof(GetPublicDecks), new { id = deckId }, deckId);
    }

}
