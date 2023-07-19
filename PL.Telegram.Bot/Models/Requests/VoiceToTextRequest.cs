using PL.Telegram.Bot.Enums;
using Telegram.Bot.Types;

namespace PL.Telegram.Bot.Models.Requests;

public class VoiceToTextRequest : BaseRequest
{
    public Voice Voice { get; set; }
    public VoiceToTextRequest(long userId, LanguageEnum language) : base(userId, language) {}
}