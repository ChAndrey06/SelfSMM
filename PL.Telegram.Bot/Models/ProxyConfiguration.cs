namespace PL.Telegram.Bot.Models;

public class ProxyConfiguration
{
    public string Address { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public bool NeedAuthorization { get; set; }
}