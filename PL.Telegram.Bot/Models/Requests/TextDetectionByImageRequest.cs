using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class TextDetectionByImageRequest : BaseRequest
{
    public string FileId { get; set; }
    
    public TextDetectionByImageRequest(long userId, LanguageEnum language) : base(userId, language) {}
}