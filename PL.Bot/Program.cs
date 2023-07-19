using Deployf.Botf;

namespace PL.Bot;

public class Program : BotfProgram
{
    public static void Main(string[] args)
    {
        StartBot(
            args, 
            onConfigure: (svc, cfg) => {},
            onRun: (app, cfg) => {}
        );
    }
    
    // public static void Main(string[] args) => StartBot(args);
}