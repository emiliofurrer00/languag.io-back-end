using Languag.io.Application.Decks;
using Microsoft.AspNetCore.Mvc;

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
}
