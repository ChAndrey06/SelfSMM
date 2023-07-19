using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PL.Telegram.Bot.Services;

public class DownloadIgApiService
{
    private const string BaseUrl = "https://instagram-media-downloader.p.rapidapi.com/rapid/";
    private readonly List<string> _apiKeys;
    private readonly HttpClient _httpClient;


    public DownloadIgApiService(IOptions<List<string>> apiKeys)
    {
        _apiKeys = apiKeys.Value;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", GetRandomApiKey());
        _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", "instagram-media-downloader.p.rapidapi.com");
    }
    
    public async Task<List<string>?> GetDownloadLinksAsync(string url)
    {
        string requestUrl;
        Func<string, List<string>?> valueGetter;

        if (url.Contains("https://www.instagram.com/p"))
        {
            requestUrl = $"{BaseUrl}post.php";
            valueGetter = j =>
            {
                var keysToCheck = new List<string> { "image", "0", "1", "2" };
                
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(j)?
                    .Where(kv => keysToCheck.Contains(kv.Key))
                    .Select(kv => kv.Value)
                    .ToList();
            };
        }
        else if (url.Contains("https://www.instagram.com/stories"))
        {
            requestUrl = $"{BaseUrl}stories.php";
            valueGetter = j =>
            {
                var jsonObj = JsonConvert.DeserializeObject<Dictionary<string, string>>(j);
                var value = jsonObj?["video"] ?? jsonObj?["image"];
                
                return value is null ? null : new List<string> { value };
            };
        }
        else
        {
            return null;
        }
        
        var response = await _httpClient.GetAsync($"{requestUrl}?url={url}");
        
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            return valueGetter(json);
        }
        catch
        {
            return null;
        }
    }
    
    private string GetRandomApiKey()
    {
        var randomIndex = new Random().Next(0, _apiKeys.Count);
        return _apiKeys[randomIndex];
    }
}