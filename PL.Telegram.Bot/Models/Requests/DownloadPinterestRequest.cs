using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class DownloadPinterestRequest : BaseRequest
{
    public string Link { get; set; }
    
    public DownloadPinterestRequest(long userId, LanguageEnum language) : base(userId, language) {}
}