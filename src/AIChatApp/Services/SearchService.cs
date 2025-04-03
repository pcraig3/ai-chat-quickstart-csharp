using System.Text.Json;

public class SearchService
{
  private readonly HttpClient _http;
  private readonly IConfiguration _config;

  public SearchService(HttpClient http, IConfiguration config)
  {
    _http = http;
    _config = config;

    var missingKeys = new List<string>();

    if (string.IsNullOrEmpty(_config["Search:Endpoint"])) missingKeys.Add("Search:Endpoint");
    if (string.IsNullOrEmpty(_config["Search:IndexName"])) missingKeys.Add("Search:IndexName");
    if (string.IsNullOrEmpty(_config["Search:ApiKey"])) missingKeys.Add("Search:ApiKey");

    if (missingKeys.Any())
    {
      throw new InvalidOperationException($"Missing required Search configuration values: {string.Join(", ", missingKeys)}");
    }
  }

  public async Task<List<string?>> GetTopChunks(string userQuery)
  {
    string searchEndpoint = _config["Search:Endpoint"]!;
    string indexName = _config["Search:IndexName"]!;
    string apiKey = _config["Search:ApiKey"]!;

    var url = $"{searchEndpoint}/indexes/{indexName}/docs/search?api-version=2024-11-01-preview";

    var requestBody = new
    {
      search = userQuery,
      top = 3,
      queryType = "simple",
      select = "chunk,title"
    };

    var request = new HttpRequestMessage(HttpMethod.Post, url)
    {
      Content = JsonContent.Create(requestBody)
    };

    request.Headers.Add("api-key", apiKey);

    var response = await _http.SendAsync(request);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
    var hits = json?.RootElement.GetProperty("value");

    var hitList = hits?.EnumerateArray().ToList() ?? new List<JsonElement>();

    return hits?.EnumerateArray()
        .Select(hit => hit.GetProperty("chunk").GetString())
        .Where(text => !string.IsNullOrWhiteSpace(text))
        .ToList() ?? new List<string?>();
  }
}