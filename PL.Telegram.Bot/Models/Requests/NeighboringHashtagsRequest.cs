using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class NeighboringHashtagsRequest : BaseRequest
{
    public string Hashtag { get; set; }
    
    public NeighboringHashtagsRequest(long userId, LanguageEnum language) : base(userId, language) {}
}