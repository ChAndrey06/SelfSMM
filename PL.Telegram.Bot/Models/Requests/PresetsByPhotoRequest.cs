using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class PresetsByPhotoRequest : BaseRequest
{
    public int PresetNumber { get; set; }
    public string FileId { get; set; }
    
    public PresetsByPhotoRequest(long userId, LanguageEnum language) : base(userId, language) {}
}