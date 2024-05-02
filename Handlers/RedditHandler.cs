namespace ORewindApi;
using Reddit;
using Reddit.Controllers;
using System.Text.RegularExpressions;


public class RedditHandler
{
    private readonly IConfiguration _configuration;
    private readonly RedditClient _reddit;

    public RedditHandler(IConfiguration configuration)
    {
        _configuration = configuration;
        _reddit = new RedditClient(
            appId: _configuration["Reddit:AppId"],
            appSecret: _configuration["Reddit:AppSecret"],
            refreshToken: _configuration["Reddit:RefreshToken"]
        );
    }

    public IEnumerable<Post> GetPostFromLastMonth(int limit = 150)
    {
        return _reddit.Subreddit("osugame").Posts.GetTop(t: "month", limit: limit);
    }

    public string? GetLinkFromBotComment(Post post, string reference)
    {
        const string pattern = @"\b(https?://[^\s'""\[\]]+)\b";
        string commentBody = post.Comments.GetTop(limit: 1)[0].Body;
        Regex regex = new Regex(pattern);
        MatchCollection matches = regex.Matches(commentBody);
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
        const string pattern = @"\b(\d+pp)\b";

        Regex regex = new Regex(pattern);

        Match match = regex.Match(title);

        return match.Success ? match.Value : null;
    }

    public IEnumerable<RedditPostInfo> ProcessPosts(int limit = 5)
    {
        var posts = GetPostFromLastMonth(limit: limit);
        return posts.Select(post => new RedditPostInfo
        {
            Description = post.Title,
            Type = post.Listing.LinkFlairText,
            VideoLink = GetLinkFromBotComment(post, "https://youtu"),
            RedditLink = "https://www.reddit.com" + post.Permalink,
            MapLink = GetLinkFromBotComment(post, "https://osu.ppy.sh/b/"),
            Pp = GetPpFromTitle(post.Title),
            ReplayLink = null,
        });
    }
}