using System.Net;

namespace PL.Telegram.Bot.Services;

public class DownloadInstagramService
{
    private const string BaseUrl = "https://saveig.app/api/ajaxSearch";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";

    public async Task<List<string>?> GetDownloadLinksAsync(string url)
    {
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "q", url },
            { "t", "media" },
            { "lang", "en" }
        });

        // using var httpClient = new HttpClient();
        using var httpClient = HttpClientFactory.GetHttpClient(true);
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        var response = await httpClient.PostAsync(BaseUrl, formContent);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        if (json is null) return null;
        
        var soup = new HtmlAgilityPack.HtmlDocument();
        soup.LoadHtml(json["data"]);
        
        var downloadItems = soup.DocumentNode.SelectNodes(".//div[contains(@class, 'download-items__btn')]/a");
        
        return downloadItems?.Select(el => WebUtility.HtmlDecode(el.GetAttributeValue("href", ""))).ToList();
    }
}