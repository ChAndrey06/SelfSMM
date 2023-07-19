using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models;

public class ClientActionsLog
{
    public int Id { get; set; }
    public string Action { get; set; }
    public long ClientId { get; set; }
    public Guid? SessionId { get; set; }
    public ScenarioEnum Scenario { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}