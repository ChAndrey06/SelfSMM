namespace PL.Telegram.Bot.Models;

public class Message
{
    public int Id { get; set; }
    public long ClientTelegramId { get; set; }
    public long ParentBotId { get; set; }
    public int ParentBotMessageId { get; set; }
    public int ChildBotMessageId { get; set; }
}