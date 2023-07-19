using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;
public class LineBreakRequest : BaseRequest
{
    public string Text { get; set; }
    
    public LineBreakRequest(long userId, LanguageEnum language) : base(userId, language) {}
}

public enum LineBreakStep
{
    FillText
}