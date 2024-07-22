using Microsoft.AspNetCore.Mvc;
using ORewindApi.Handlers;
namespace ORewindApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OsuApiController : ControllerBase
{
    private readonly OsuApiHandler _osuHandler;
    private readonly ILogger<OsuApiController> _logger;
    public OsuApiController(ILogger<OsuApiController> logger, OsuApiHandler osuHandler)
    {
        _logger = logger;
        _osuHandler = osuHandler;
    }
    [HttpGet("beatmapsetidfromdiffid")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBeatmapsetIdFromDiff(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 9)
        {
            return BadRequest("Something wrong with your id");
        }
        try
        {
            string result = await _osuHandler.GetBeatmapsetIdFromDiff(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest("Request failed with this message: " + ex.Message);
        }
    }
    [HttpGet("userosuinfo")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserOsuInfo(string input)
    {
        if (string.IsNullOrEmpty(input) || input.Length > 35)
        {
            return BadRequest("Something wrong with your id");
        }
        try
        {
            UserInfoResponse result = await _osuHandler.GetUserInfo(input);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest("Request failed with this message: " + ex.Message);
        }
    }
}