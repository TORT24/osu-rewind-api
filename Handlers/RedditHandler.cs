namespace ORewindApi.Handlers;
using Reddit;
using Reddit.Controllers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;


public class RedditHandler
{
    private readonly IConfiguration _configuration;
    private readonly RedditClient _reddit;
    private readonly Regex _ppRegex;
    private readonly Regex _linkRegex;
    private readonly Subreddit _osugame;
    private readonly ILogger<RedditHandler> _logger;


    public RedditHandler(IConfiguration configuration, ILogger<RedditHandler> logger)
    {
        _configuration = configuration;
        _reddit = new RedditClient(
            appId: _configuration["Reddit:AppId"],
            appSecret: _configuration["Reddit:AppSecret"],
            refreshToken: _configuration["Reddit:RefreshToken"],
            accessToken: _configuration["Reddit:AccessToken"]
        );
        _ppRegex = new Regex(@"\b(\d+pp)\b");
        _linkRegex = new Regex(@"\b(https?://[^\s'""\[\]]+)\b");
        _osugame = _reddit.Subreddit("osugame");
        _logger = logger;
    }

    public void StatusCheck()
    {
        try
        {
            _logger.LogInformation("Checking if API works:");
            _logger.LogInformation(_osugame.Posts.GetTop(t: "month", limit: 1)[0].Title);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Either you didn't define your Reddit credential or idk", ex);
        }
    }
    public string? GetLinkFromBotComment(string commentBody, string reference)
    {
        MatchCollection matches = _linkRegex.Matches(commentBody);
        foreach (Match match in matches)
        {
            string url = match.Groups[1].Value;
            if (url.Contains(reference))
            {
                return url;
            }
        }

        return null;
    }
    public string? GetPpFromTitle(string title)
    {
        Match match = _ppRegex.Match(title);
        return match.Success ? match.Value : null;
    }
    public IEnumerable<RedditPostInfo> ProcessPosts(IEnumerable<Post> posts)
    {
        var postInfos = new ConcurrentBag<RedditPostInfo>();
        // Using Parallel.ForEach for concurrent processing
        Parallel.ForEach(posts, post =>
        {
            var topCommentBody = post.Comments.GetTop(limit: 1)[0].Body;
            var redditPostInfo = new RedditPostInfo
            {
                Description = post.Title,
                Type = post.Listing.LinkFlairText,
                VideoLink = GetLinkFromBotComment(topCommentBody, "https://youtu"),
                RedditLink = "https://www.reddit.com" + post.Permalink,
                MapLink = GetLinkFromBotComment(topCommentBody, "https://osu.ppy.sh/b/"),
                Pp = GetPpFromTitle(post.Title),
                ReplayLink = null,
                DatePosted = post.Created,
            };
            postInfos.Add(redditPostInfo);
        });

        // Stop the stopwatch and log the elapsed time
        return postInfos;
    }

    public List<RedditPostInfo> GetPostsFromLastMonth(int limit)
    {
        List<RedditPostInfo> allPosts = [];
        string lastPostId = string.Empty;
        int remainingLimit = limit;
        // Initialize the stopwatch
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        while (remainingLimit > 0)
        {
            var posts = _osugame.Posts.GetTop(t: "month", limit: remainingLimit, after: lastPostId);
            lastPostId = "t3_" + posts.Last().Id;
            remainingLimit -= posts.Count;
            allPosts.AddRange(ProcessPosts(posts));
            _logger.LogInformation("{Time}: Posts parsed: {ParsedCount}/{Limit}", DateTime.Now.ToLongTimeString(), allPosts.Count, limit);
        }
        stopwatch.Stop();
        _logger.LogInformation("ProcessPosts method completed in {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        return allPosts;
    }

    public IEnumerable<RedditPostInfo> GetPostsFor2024(int limit = 100, bool strictLimit = false, int? month = null)
    {
        if (month != null & (month > DateTime.Now.Month || month < 1))
        {
            return [];
        }
        List<RedditPostInfo> allPosts = [];
        string lastPostId = string.Empty;
        int remainingLimit = limit;
        while (allPosts.Count < limit)
        {
            var posts = _osugame.Posts.GetTop(t: "year", limit: remainingLimit, after: lastPostId).Where(post => post.Created.Year == 2024);
            if (month != null)
            {
                posts = posts.Where(post => post.Created.Month == month);
            }
            if (!posts.Any()) continue;
            lastPostId = "t3_" + posts.Last().Id;
            remainingLimit -= posts.Count();
            allPosts.AddRange(ProcessPosts(posts));
            if (remainingLimit < 50)
            {
                remainingLimit = 50; // doing this to reduce the number of requests to reddit api
            }
            _logger.LogInformation("{Time}: Posts parsed: {ParsedCount}/{Limit}", DateTime.Now.ToLongTimeString(), allPosts.Count, limit);
        }
        return strictLimit ? allPosts.Take(limit) : allPosts;
    }

}