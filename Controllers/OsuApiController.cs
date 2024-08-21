using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORewindApi.Handlers;
namespace ORewindApi.Controllers;

[Authorize]
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
    [HttpGet("verifybeatmap")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyBeatmap(string url)
    {
        if (string.IsNullOrEmpty(url) || url.Length > 60)
        {
            return BadRequest("Something wrong with your url");
        }
        try
        {
            string result = await _osuHandler.VerifyBeatmap(url);
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
    public async Task<IActionResult> GetUserInfoEndpoint(string input)
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
    [HttpGet("handleosuinfo")]
    [ProducesResponseType(typeof(SuggestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleOsuInfoEndpoint(string? map, string? user)
    {
        string? beatmapSetResponse = null;
        UserInfoResponse? userInfoResponse = null;
        if (string.IsNullOrEmpty(map) && string.IsNullOrEmpty(user))
        {
            return BadRequest("There's no inputs");
        }
        if (user?.Length > 45 || map?.Length > 60)
        {
            return BadRequest("Your inputs suck");
        }
        try
        {
            if (!string.IsNullOrEmpty(map))
                beatmapSetResponse = await _osuHandler.VerifyBeatmap(map);
            if (!string.IsNullOrEmpty(user))
                userInfoResponse = await _osuHandler.GetUserInfo(user);
            var suggestResponse = new SuggestResponse()
            {
                BeatMapSetId = beatmapSetResponse,
                UserInfo = userInfoResponse
            };
            return Ok(suggestResponse);
        }
        catch (Exception ex)
        {
            return BadRequest("Request failed with this message: " + ex.Message);
        }
    }
}