using System.Net.Http.Json;
using System.Text.Json;

public class SearchService
{
  private readonly HttpClient _http;
  private readonly IConfiguration _config;

  public SearchService(HttpClient http, IConfiguration config)
  {
    _http = http;
    _config = config;
  }

  public async Task<List<string?>> GetTopChunks(string userQuery)
  {
    string searchEndpoint = _config["Search:Endpoint"];
    string indexName = _config["Search:IndexName"];
    string apiKey = _config["Search:ApiKey"];  // or use a managed identity header

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