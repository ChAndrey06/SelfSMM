// // var builder = WebApplication.CreateBuilder(args);
// // var app = builder.Build();
// //
// // app.MapGet("/", () => "Hello World!");
// //
// // app.Run();
//
// using Telegram.Bot;
// using Telegram.Bot.Args;
// using Telegram.Bot.Polling;
// using Telegram.Bot.Types;
// using Telegram.Bot.Types.Enums;
//
// namespace BotTest;
//
// class Program
// {
//     private static ITelegramBotClient client;	
//     void Main(string[] args)
//     {
//         client = new TelegramBotClient("6177991857:AAGYAslrR6Up3aQGUByyTMxYEThl_--5eqo");
//         // client.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
//         //
//         using CancellationTokenSource cts = new ();
//
//         ReceiverOptions receiverOptions = new ()
//         {
//             AllowedUpdates = Array.Empty<UpdateType>()
//         };
//
//         client.StartReceiving(
//             updateHandler: HandleUpdateAsync,
//             pollingErrorHandler: HandleErrorAsync,
//             receiverOptions: receiverOptions,
//             cancellationToken: cts.Token
//         );
//         
//         Console.ReadLine();
//     }
//
//     public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
//     {
//         var handler = update switch
//         {
//             { Message: { } message } => BotOnMessageReceived(message, cancellationToken)
//         };
//         
//         await handler;
//     }
//     public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
//     {
//         // Данный Хендлер получает ошибки и выводит их в консоль в виде JSON
//         Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
//     }
//     
//     private static async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
//     {
//         if (message.Text is { } messageText)
//         {
//             var action = messageText.Split(' ')[0] switch
//             {
//                 "/start" => Start(client, message, cancellationToken),
//             };
//             await action;
//         }
//         
//         static async Task Start(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
//         {
//             await botClient.SendTextMessageAsync
//             (
//                 chatId: message.Chat.Id,
//                 text: "text",
//                 cancellationToken: cancellationToken
//             );
//             
//             await Task.Delay(20000, cancellationToken: cancellationToken);
//         }
//     }
// }
//


using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("");

using CancellationTokenSource cts = new ();

// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new ()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    // Echo received message text
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: "You said:\n" + messageText,
        cancellationToken: cancellationToken);

    await Task.Delay(20000);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}