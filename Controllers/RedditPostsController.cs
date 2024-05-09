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

    [HttpGet("lastmonthposts")]
    [ProducesResponseType(typeof(List<RedditPostInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IResult GetRedditPostsForLastMonth(int limit = 5)
    {
        return Results.Ok(_redditHandler.GetPostFromLastMonth(limit));
    }
    
    [HttpGet("2024posts")]
    [ProducesResponseType(typeof(IEnumerable<RedditPostInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IResult GetRedditPostsForLastYear(int limit = 1000, bool strictLimit = false, int? month = null)
    {
        return Results.Ok(_redditHandler.GetPostsFor2024(limit, strictLimit, month));
    }
}