using Deployf.Botf;

namespace PL.Bot.Controllers;

public class MainController : BotController
{
    [Action("/start")]
    private async Task  Start()
    {
        Push($"Start message");
        
        RowButton($"💰 Create a post", Q(ButtonHandler, $"btn1"));
        RowButton($"💰 Write content plan", Q(ButtonHandler, $"btn2"));
        RowButton($"💰 Download from instagram/tiktok", Q(ButtonHandler, "btn3"));
        RowButton($"Download from pinterest", Q(ButtonHandler, "btn4"));
        
        await Send();
    }
    
    [Action]
    private async Task ButtonHandler(string str)
    {
        Push($"{str} pressed");
        await Send();
        
        
    }
}