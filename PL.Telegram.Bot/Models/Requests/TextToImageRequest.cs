using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class TextToImageRequest : BaseRequest
{
    public string Text { get; set; }
    public string FileId  { get; set; }
    
    public TextToImageRequest(long userId, LanguageEnum language) : base(userId, language) {}
}

public enum TextToImageStep
{
    FillText,
    FillImg,
    FillAction
}