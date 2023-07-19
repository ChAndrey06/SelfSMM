using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models.Requests;

public class HashtagsByPhotoRequest : BaseRequest
{
    public string FileId { get; set; }
    
    public HashtagsByPhotoRequest(long userId, LanguageEnum language) : base(userId, language) {}
}