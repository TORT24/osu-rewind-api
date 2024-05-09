namespace ORewindApi;

public class RedditPostInfo
{
    public required string Type { get; init; }
    public required string Description { get; init; }
    public string? VideoLink { get; init; }
    public string? MapLink { get; init; }
    public string? Pp { get; init; }
    public required string RedditLink { get; init; }
    public string? ReplayLink { get; init; }
    public required DateTime DatePosted { get; init; }
}