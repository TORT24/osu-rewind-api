using System.Net.Http.Headers;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
namespace ORewindApi.Handlers;
public class OsuApiHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OsuApiHandler> _logger;
    private DateTime _expirationTime;
    private string _accessToken = "";
    private readonly HttpClient _httpClient;
    public OsuApiHandler(IConfiguration configuration, ILogger<OsuApiHandler> logger, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://osu.ppy.sh/api/v2/");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    public async Task<string> VerifyBeatmap(string url)
    {
        string mapsetId;
        if (CheckStringForLink(url) && !string.IsNullOrEmpty(url))
        {
            url = url.EndsWith('/') ? url[..^1] : url;
            if (url.Contains("/beatmapsets/"))
            {
                var splitedLink = url.Split('/');
                int mapIdIndex = Array.FindIndex(splitedLink, x => x.Equals("beatmapsets")) + 1;
                mapsetId = splitedLink[mapIdIndex];
                await RefreshAccessTokenIfNeededAsync();
                string endpoint = $"beatmapsets/{mapsetId}";
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
                var response = await _httpClient.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<BeatmapsetLookupResponse>(responseBody);
                    mapsetId = responseObj.Id;
                }
                else
                    throw new Exception("Request failed with status code: " + response.StatusCode);
            }
            else if (url.Contains("/b/") || url.Contains("/beatmaps/"))
            {
                var splitedLink = url.Split('/');
                var diffId = splitedLink[^1];
                mapsetId = await GetBeatmapsetIdFromDiff(diffId);
            }
            else
            {
                throw new Exception("That's a wrong link");
            }
            return mapsetId;
        }
        throw new Exception("There's no link in your input");
    }
    public async Task<UserInfoResponse> GetUserInfo(string input)
    {
        bool inputIsLink = CheckStringForLink(input);
        string user;
        string keyType = inputIsLink ? "id" : "username";
        if (inputIsLink)
        {
            input = input.EndsWith('/') ? input[..^1] : input;
            var splitLink = input.Split("/");
            // if link ends with "osu" instead of id, we take value from pre-last index
            user = int.TryParse(splitLink[^1], out _) ? splitLink[^1] : splitLink[^2];
        }
        else
            user = input;
        await RefreshAccessTokenIfNeededAsync();
        string endpoint = $"users/{user}?key={keyType}";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<UserInfoResponse>(responseBody);
            return responseObj;
        }
        else
        {
            throw new Exception("Request failed with status code: " + response.StatusCode);
        }
    }

    public async Task<string> GetBeatmapsetIdFromDiff(string beatmapId)
    {
        await RefreshAccessTokenIfNeededAsync();
        string endpoint = $"beatmaps/lookup?id={beatmapId}";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<BeatmapLookupResponse>(responseBody);
            return responseObj.Beatmapset_id!;
        }
        else
        {
            throw new Exception("Request failed with status code: " + response.StatusCode);
        }
    }

    public async Task RefreshAccessTokenIfNeededAsync()
    {
        if (DateTime.UtcNow > _expirationTime)
        {
            try
            {
                var clientId = _configuration["OsuApi:ClientID"];
                var secretKey = _configuration["OsuApi:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("Client ID or Secret Key is not assigned.");
                }

                var tokenApiResponse = await GetTokenAsync(clientId, secretKey);

                if (tokenApiResponse == null)
                {
                    throw new InvalidOperationException("Failed to retrieve token.");
                }

                _expirationTime = DateTime.UtcNow.AddSeconds(tokenApiResponse.Expires_in);
                _accessToken = tokenApiResponse.Access_token;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error refreshing access token: {ex.Message}");
            }
        }
    }

    private async Task<TokenResponse> GetTokenAsync(string clientID, string secretKey)
    {
        string apiUrl = "https://osu.ppy.sh/oauth/token";
        var requestData = new Dictionary<string, string>
        {
            { "client_id", clientID },
            { "client_secret", secretKey },
            { "grant_type", "client_credentials" },
            { "scope", "public" }
        };
        var content = new FormUrlEncodedContent(requestData);
        using HttpClient http = new();
        var response = await http.PostAsync(apiUrl, content);

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<TokenResponse>(responseBody);
            return responseObj!;
        }
        else
        {
            throw new Exception("Request failed with status code: " + response.StatusCode);
        }
    }

    public bool CheckStringForLink(string input)
    {
        Uri? uriResult;
        bool result = Uri.TryCreate(input, UriKind.Absolute, out uriResult)
    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        return result;
    }
}
public class TokenResponse
{
    public required string Access_token { get; init; }
    public required string Token_type { get; init; }
    public required int Expires_in { get; init; }
}
public class BeatmapLookupResponse
{
    public required string Beatmapset_id { get; set; }
}

public class UserInfoResponse
{
    public required string Avatar_url { get; init; }
    public required string Username { get; init; }
    public required int Id { get; init; }
}

public class SuggestResponse
{
    public string? BeatMapSetId { get; init; }
    public UserInfoResponse? UserInfo { get; init; }
}

public class BeatmapsetLookupResponse
{
    public required string Id { get; init; }
}