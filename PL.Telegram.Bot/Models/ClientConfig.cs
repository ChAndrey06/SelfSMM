namespace PL.Telegram.Bot.Models;

public class ClientConfig
{
    public long Id { get; set; }
    public string ApiId { get; set; }
    public string ApiHash { get; set; }
    public string PhoneNumber { get; set; }
    public string FirstName { get; set; } = "FirstName";
    public string LastName { get; set; } = "LastName";
    public string Password { get; set; }
    public bool Banned { get; set; } = false;
    public bool InUse { get; set; } = false;
}