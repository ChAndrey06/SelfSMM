namespace PL.Telegram.Bot.Models;

public class GptConfig
{
    public string ApiKey { get; set; }
    public string Model { get; set; }
    public float? Temperature { get; set; }
    public int? MaxTokens { get; set; } 
}