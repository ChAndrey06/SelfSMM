using PL.Telegram.Bot.Abstract;
using Telegram.Bot;

namespace PL.Telegram.Bot.Services;

public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService
    (
        ITelegramBotClient botClient,
        UpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<UpdateHandler>> logger
    ) : base(botClient, updateHandler, logger)
    {
    }
}