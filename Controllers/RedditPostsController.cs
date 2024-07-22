using Microsoft.AspNetCore.Mvc;
using ORewindApi.Handlers;
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
    public IResult GetRedditPostsForLastMonthEndpoint(int limit = 5)
    {
        if (limit < 1 || limit > 5000){
            return Results.BadRequest("Choose limit between 1 and 5000");   
        }
        return Results.Ok(_redditHandler.GetPostsFromLastMonth(limit));
    }
    
    [HttpGet("2024posts")]
    [ProducesResponseType(typeof(IEnumerable<RedditPostInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IResult GetPostsFor2024Endpoint(int limit = 1000, bool strictLimit = false, int? month = null)
    {
        if (month != null & (month > DateTime.Now.Month || month < 1))
        {
            return Results.BadRequest($"Choose month betwen 1 and {DateTime.Now.Month}");
        }
        if (limit < 1 || limit > 5000)
        {
            return Results.BadRequest("Choose limit between 1 and 5000");
        }
        return Results.Ok(_redditHandler.GetPostsFor2024(limit, strictLimit, month));
    }
}