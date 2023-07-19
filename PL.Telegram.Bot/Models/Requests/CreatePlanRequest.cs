using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class CreatePlanRequest : BaseRequest
{
    public string Topic { get; set; }
    public string Audience { get; set; }
    public string Period { get; set; }
    
    public CreatePlanRequest(long userId, LanguageEnum language) : base(userId, language) {}
}