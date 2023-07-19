using PL.Telegram.Bot.Abstract;

namespace PL.Telegram.Bot.Services;

public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService
    (
        IServiceProvider serviceProvider, 
        ILogger<PollingService> logger
    ) : base(serviceProvider, logger)
    {
    }
}