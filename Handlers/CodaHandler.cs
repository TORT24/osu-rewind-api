using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace ORewindApi.Handlers;
public class CodaHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CodaHandler> _logger;
    private DateTime _expirationTime;
    private readonly HttpClient _httpClient;
    private string _accessToken = "";
    private string AccessToken
    {
        get { return _accessToken; }
        set
        {
            _accessToken = value;
            if (_httpClient.DefaultRequestHeaders.Contains("c-token"))
            {
                _httpClient.DefaultRequestHeaders.Remove("c-token");
            }
            _httpClient.DefaultRequestHeaders.Add("c-token", value);
        }
    }

    public CodaHandler(IConfiguration configuration, ILogger<CodaHandler> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://btmc.live/coda/");
    }

    public async Task MoveSuggestionToCodaAsync(Suggestion suggestion)
    {
        PropertyInfo[] properties = suggestion.GetType().GetProperties();
        var cells = new List<CodaCell>();

        foreach (PropertyInfo property in properties)
        {
            if (property.GetValue(suggestion) is string value)
            {
                var newCodaColumn = new CodaCell
                {
                    Column = property.Name,
                    Value = value
                };
                cells.Add(newCodaColumn);
            }
        }
        if (cells.Count > 0)
        {
            await RefreshAccessTokenIfNeededAsync();
            string? docID = _configuration["CodaApi:DocID"];
            string? tableID = _configuration["CodaApi:TableID"];
            if (string.IsNullOrEmpty(docID) || string.IsNullOrEmpty(tableID))
                throw new InvalidOperationException("Coda table data is not defined.");
            string endpoint = "/coda/api/docs/doc/table/rows/upsert";
            var requestBody = new
            {
                content = new[]
                {
                    new
                    {
                        cells
                    }
                }
            };

            string suggetDataJson = JsonConvert.SerializeObject(requestBody, new JsonSerializerSettings
            { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented });
            var requestData = new Dictionary<string, string>
            {
                { "docID", docID },
                { "tableID", tableID },
                { "data", suggetDataJson }
            };
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new FormUrlEncodedContent(requestData),
            };
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("New record was made in Coda");
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Request failed with status code: {response.StatusCode}");
            }
        }
    }

    public async Task<bool> VerifyTokenAsyc()
    {
        await RefreshAccessTokenIfNeededAsync();
        string endpoint = "auth/validate/token";
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        var response = await _httpClient.SendAsync(requestMessage);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var responseObj = JsonConvert.DeserializeObject<CodaKeyRequestResponse>(responseBody);
            return responseObj.Validated;
        }
        else
        {
            return false;
        }

    }

    public async Task RefreshAccessTokenIfNeededAsync()
    {
        if (DateTime.UtcNow > _expirationTime)
        {
            try
            {
                string? login = _configuration["CodaApi:Login"];
                string? password = _configuration["CodaApi:Password"];
                var endpoint = "auth/authenticate";
                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    throw new Exception("Login and Password are not assigned.");
                }
                var requestData = new Dictionary<string, string>
                {
                    { "login", login },
                    { "password", password },
                };

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new FormUrlEncodedContent(requestData),
                };
                var response = await _httpClient.SendAsync(requestMessage);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonConvert.DeserializeObject<CodaKeyRequestResponse>(responseBody);
                    if (responseObj.Validated == true)
                    {
                        AccessToken = responseObj!.Data!.Token;
                        _expirationTime = DateTime.UtcNow.AddDays(1);
                        _logger.LogInformation("Refreshed token!");
                    }
                    else
                    {
                        throw new Exception("Validation failed!");
                    }
                }
                else
                {
                    throw new Exception("Request failed with status code: " + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error refreshing access token: {ex.Message}");
            }
        }
    }
    public class CodaKeyRequestResponse
    {
        public required bool Validated { get; set; }
        public CodaKeyRequestResponseData? Data { get; init; }
    }

    public class CodaKeyRequestResponseData
    {
        public required string Text { get; set; }
        public required string Token { get; set; }
    }

    public class CodaCell
    {
        public required string Column { get; set; }
        public required string Value { get; set; }
    }
}

public class Suggestion
{
    public required string Description { get; init; }
    public required string Type { get; init; }
    public required string SourceLink { get; init; }
    public required string Date { get; init; }
    public string? MapLink { get; init; }
    public string? Player { get; init; }
    public string? PlayerLink { get; init; }
    public string? Pp { get; init; }
    public string? MadeBy { get; init; }
}