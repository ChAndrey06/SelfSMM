using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;
public class DownloadIgTtRequest : BaseRequest
{
    public string Link { get; set; }
    
    public DownloadIgTtRequest(long userId, LanguageEnum language) : base(userId, language) {}
}