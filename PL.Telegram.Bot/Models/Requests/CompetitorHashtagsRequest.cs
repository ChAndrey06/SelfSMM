using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class CompetitorHashtagsRequest : BaseRequest
{
    public string Username { get; set; }
    
    public CompetitorHashtagsRequest(long userId, LanguageEnum language) : base(userId, language) {}
}