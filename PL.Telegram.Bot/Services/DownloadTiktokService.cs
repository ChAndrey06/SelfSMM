using HtmlAgilityPack;

namespace PL.Telegram.Bot.Services;


public class DownloadTiktokService
{
    private const string BaseUrl = "https://ssstik.io/";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36";

    public async Task<Stream?> GetDownloadLinksAsync(string url)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(BaseUrl);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        var soup = new HtmlDocument();
        soup.LoadHtml(html);

        var jsNode = soup.DocumentNode.SelectSingleNode("//script[contains(text(), 'form.setAttribute(\"include-vals\",')]");

        if (jsNode is null) return null;

        var jsCode = jsNode.InnerText;
        var valueStartIndex = jsCode.IndexOf("tt:'", StringComparison.Ordinal) + 4;
        var valueEndIndex = jsCode.IndexOf("'", valueStartIndex, StringComparison.Ordinal);
        var ttValue = jsCode.Substring(valueStartIndex, valueEndIndex - valueStartIndex);

        var postUrl = $"{BaseUrl}abc?url=dl";
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "id", url },
            { "locale", "en" },
            { "tt", ttValue }
        });

        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        response = await httpClient.PostAsync(postUrl, formContent);
        response.EnsureSuccessStatusCode();
        
        html = await response.Content.ReadAsStringAsync();
        soup.LoadHtml(html);

        var downloadLinkNode = soup.DocumentNode.SelectSingleNode("//div[contains(@class, 'result_overlay_buttons')]/a");
        var downloadLink = downloadLinkNode?.GetAttributeValue("href", "");

        if (downloadLink is null) return null;
        
        response = await httpClient.GetAsync(downloadLink);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStreamAsync();
    }
}
