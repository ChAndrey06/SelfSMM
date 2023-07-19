using PL.Telegram.Bot.Enums;
using Telegram.Bot.Types;

namespace PL.Telegram.Bot.Models.Requests;

public class InterruptibleRequest : BaseRequest
{
    public CallbackQuery CallbackQuery { get; set; }
    public InterruptibleRequest(long userId, LanguageEnum language) : base(userId, language) {}
}