using PL.Telegram.Bot.Enums;

namespace PL.Telegram.Bot.Models;

public class UserState
{
    public User User { get; set; }
    public ScenarioEnum Scenario { get; set; }
    public string Step { get; set; }
    public BaseRequest Request { get; set; }
}