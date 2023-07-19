using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class CreatePostRequest : BaseRequest
{
    public string Topic { get; set; }
    public string For { get; set; }
    
    public CreatePostRequest(long userId, LanguageEnum language) : base(userId, language) {}
}