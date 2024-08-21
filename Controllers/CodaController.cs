using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORewindApi.Handlers;
namespace ORewindApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CodaController : ControllerBase
{
    private readonly CodaHandler _codaHandler;
    private readonly ILogger<CodaController> _logger;
    public CodaController(ILogger<CodaController> logger, CodaHandler codaHandler)
    {
        _logger = logger;
        _codaHandler = codaHandler;
    }
    [HttpPost("postSuggestionToCoda")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostSuggestionToCoda([FromQuery]Suggestion suggestion)
    {
        try
        {
            await _codaHandler.MoveSuggestionToCodaAsync(suggestion);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}