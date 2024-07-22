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

    public async Task<UserInfoResponse> GetUserInfo(string input)
    {
        bool inputIsLink = CheckStringForLink(input);
        string user;
        string keyType = inputIsLink ? "id" : "username";
        if (inputIsLink)
        {
            var splitLink = input.Split("/");
            user = splitLink[^1].Length > 0 ? splitLink[^1] : splitLink[^2];
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
            var responseObj = JsonConvert.DeserializeObject<BeatmapSetResponse>(responseBody);
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
        var requestData = new List<KeyValuePair<string, string>>
        {
            new ("client_id", clientID),
            new ("client_secret", secretKey),
            new ("grant_type", "client_credentials"),
            new ("scope", "public")
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
public class BeatmapSetResponse
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
    public string? BeatmapSetResponse{ get; init; }
    public UserInfoResponse? UserInfoResponse{ get; init; }
}
