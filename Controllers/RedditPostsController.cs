using Microsoft.AspNetCore.Mvc;

namespace ORewindApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedditPostsController : ControllerBase
{
    private readonly RedditHandler _redditHandler;
    private readonly ILogger<RedditPostsController> _logger;
    public RedditPostsController(ILogger<RedditPostsController> logger, RedditHandler redditHandler)
    {
        _logger = logger;
        _redditHandler = redditHandler;
    }

    [HttpGet(Name = "GetRedditPosts")]
    [ProducesResponseType(typeof(IEnumerable<RedditPostInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IResult Get()
    {
        return Results.Ok(_redditHandler.ProcessPosts(5));
    }
}