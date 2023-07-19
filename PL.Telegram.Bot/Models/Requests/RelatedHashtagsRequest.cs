using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class RelatedHashtagsRequest : BaseRequest
{
    public string Hashtag { get; set; }
    
    public RelatedHashtagsRequest(long userId, LanguageEnum language) : base(userId, language) {}
}