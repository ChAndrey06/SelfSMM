using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models;

public class BaseRequest
{
    public long UserId { get; set; }
    public LanguageEnum Language { get; set; }
    
    public BaseRequest(long userId, LanguageEnum language)
    {
        UserId = userId;
        Language = language;
    }
}