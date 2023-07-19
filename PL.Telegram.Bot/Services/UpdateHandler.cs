using System.Net;
using PL.Telegram.Bot.Enums;
using PL.Telegram.Bot.Models;
using PL.Telegram.Bot.Models.Requests;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenAI.ObjectModels.RequestModels;
using KeyboardButton = Telegram.Bot.Types.ReplyMarkups.KeyboardButton;
using Message = Telegram.Bot.Types.Message;
using ReplyKeyboardMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup;
using Update = Telegram.Bot.Types.Update;

namespace PL.Telegram.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly WClientsService _clientsService;
    private readonly Dictionary<long, UserState> _states;
    private readonly AppDbContext _dbContext;
    private readonly TranslateService _translate;
    private readonly ChatGptService _gptService;
    private readonly DownloadIgApiService _igService;
    private readonly DownloadTiktokService _ttService;
    
    private const string QronpayBaseUrl = "https://qronpay-dev-crm.spaceapp.ru";

    public UpdateHandler
    (
        ITelegramBotClient botClient,
        WClientsService clientsService,
        AppDbContext dbContext,
        ChatGptService chatGptService,
        DownloadIgApiService igService,
        DownloadTiktokService ttService
    ) 
    {
        _botClient = botClient;
        _clientsService = clientsService;
        _states = new Dictionary<long, UserState>();
        _dbContext = dbContext;
        _translate = new TranslateService();
        _gptService = chatGptService;
        _igService = igService;
        _ttService = ttService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var tgId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? 0;

        if (!_states.ContainsKey(tgId))
        {
            var user = await GetOrCreateUserAsync(tgId);
            
            _states[tgId] = new UserState
            {
                User = user
            };
            _translate.Language = user.Language;
        }
        
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message }                       => BotOnMessageReceived(message, cancellationToken),
            // { EditedMessage: { } message }                 => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery }           => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            // { InlineQuery: { } inlineQuery }               => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            // { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _                                              => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is { } messageText)
        {
            var action = messageText.Split(' ')[0] switch
            {
                "/start"              => Start(message, cancellationToken),
                "/change_lang"        => ChangeLanguage(message, cancellationToken),
                "/subscribe"          => Subscribe(message, cancellationToken),
                "/check_subscription" => CheckSubscription(message, cancellationToken),
                _                     => ScenarioHandler(message, cancellationToken)
            };
            await action;
        }
        else
        {
            await ScenarioHandler(message, cancellationToken);
        }
    }
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ScenarioEnum>(callbackQuery.Data, out var scenario))
        {
            scenario = ScenarioEnum.Interruptible;
        }
        
        switch (scenario) 
        {
            case ScenarioEnum.LineBreak:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.LineBreakInitMessage,
                    cancellationToken: cancellationToken
                );

                var lineBreakState = _states[callbackQuery.From!.Id];
                lineBreakState.Scenario = ScenarioEnum.LineBreak;
                lineBreakState.Step = LineBreakStep.FillText.ToString();
            }
            break;
            case ScenarioEnum.TextToImage:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.TextToImageInitMessage,
                    cancellationToken: cancellationToken
                );

                var state = _states[callbackQuery.From!.Id];
                state.Scenario = ScenarioEnum.TextToImage;
                state.Step = TextToImageStep.FillText.ToString();
            }
            break;
            case ScenarioEnum.DownloadIgTt:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.DownloadIgTtInitMessage,
                    cancellationToken: cancellationToken
                );

                var downloadIgTtState = _states[callbackQuery.From!.Id];
                downloadIgTtState.Scenario = ScenarioEnum.DownloadIgTt;
                downloadIgTtState.Step = "FillLink";
            }
            break;
            case ScenarioEnum.TextDetectionByImage:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.TextDetectionByImageInitMessage,
                    cancellationToken: cancellationToken
                );

                var textDetectionByImageState = _states[callbackQuery.From!.Id];
                textDetectionByImageState.Scenario = ScenarioEnum.TextDetectionByImage;
                textDetectionByImageState.Step = "FillImage";
            }
            break;
            case ScenarioEnum.DownloadPinterest:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.DownloadPinterestInitMessage,
                    cancellationToken: cancellationToken
                );

                var downloadPinterestState = _states[callbackQuery.From!.Id];
                downloadPinterestState.Scenario = ScenarioEnum.DownloadPinterest;
                downloadPinterestState.Step = "FillLink";
                break;
            case ScenarioEnum.CreatePost:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.CreatePostFillTopic,
                    cancellationToken: cancellationToken
                );

                var createPostState = _states[callbackQuery.From!.Id];
                createPostState.Scenario = ScenarioEnum.CreatePost;
                createPostState.Step = "FillTopic";
            }
            break;
            case ScenarioEnum.CreatePlan:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.CreatePlanFillTopic,
                    cancellationToken: cancellationToken
                );

                var contentPlanState = _states[callbackQuery.From!.Id];
                contentPlanState.Scenario = ScenarioEnum.CreatePlan;
                contentPlanState.Step = "FillTopic";
                break;
            case ScenarioEnum.RelatedHashtags:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.RelatedHashtagsInitMessage,
                    cancellationToken: cancellationToken
                );

                var relatedHashtagsState = _states[callbackQuery.From!.Id];
                relatedHashtagsState.Scenario = ScenarioEnum.RelatedHashtags;
                relatedHashtagsState.Step = "FillHashtag";
                break;
            case ScenarioEnum.VoiceMessageToText:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.VoiceMessageToTextInitMessage,
                    cancellationToken: cancellationToken
                );

                var voiceToTextState = _states[callbackQuery.From!.Id];
                voiceToTextState.Scenario = ScenarioEnum.VoiceMessageToText;
                voiceToTextState.Step = "FillVoice";
                break;
            case ScenarioEnum.CompetitorHashtags:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.CompetitorHashtagsInitMessage,
                    cancellationToken: cancellationToken
                );

                var competitorHashtagsState = _states[callbackQuery.From!.Id];
                competitorHashtagsState.Scenario = ScenarioEnum.CompetitorHashtags;
                competitorHashtagsState.Step = "FillUsername";
                break;
            case ScenarioEnum.NeighboringHashtags:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.NeighboringHashtagsInitMessage,
                    cancellationToken: cancellationToken
                );

                var neighboringHashtagsState = _states[callbackQuery.From!.Id];
                neighboringHashtagsState.Scenario = ScenarioEnum.NeighboringHashtags;
                neighboringHashtagsState.Step = "FillHashtag";
                break;
            case ScenarioEnum.HashtagsByPhoto:
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.HashtagsByPhotoInitMessage,
                    cancellationToken: cancellationToken
                );

                var hashtagsByPhotoState = _states[callbackQuery.From!.Id];
                hashtagsByPhotoState.Scenario = ScenarioEnum.HashtagsByPhoto;
                hashtagsByPhotoState.Step = "FillPhoto";
                break;
            case ScenarioEnum.RandomNumberGenerator:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.RandomNumberGeneratorInitMessage,
                    cancellationToken: cancellationToken
                );

                var randomNumberGeneratorState = _states[callbackQuery.From!.Id];
                randomNumberGeneratorState.Scenario = ScenarioEnum.RandomNumberGenerator;
                randomNumberGeneratorState.Step = "FillRange";
            }
            break;
            case ScenarioEnum.PresetsByPhoto:
            {
                await _botClient.SendTextMessageAsync
                (
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: _translate.PresetsByPhotoInitMessage,
                    cancellationToken: cancellationToken
                );

                var state = _states[callbackQuery.From!.Id];
                state.Scenario = ScenarioEnum.PresetsByPhoto;
                state.Step = "FillPhoto";
            }
            break;
            case ScenarioEnum.Interruptible:
            {
                var state = _states[callbackQuery.From!.Id];
                state.Scenario = ScenarioEnum.Interruptible;

                var request = new InterruptibleRequest(callbackQuery.Message!.Chat.Id, state.User.Language)
                {
                    CallbackQuery = callbackQuery
                };
                
                _clientsService.StartInterruptibleScenario(state.Scenario, request);
            }
            break;  
        }
    }
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    private async Task<Models.User> GetOrCreateUserAsync(long telegramId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);

        if (user is not null)
        {
            user.Payments = await _dbContext.Payments.Where(p => p.UserId == user.Id).ToListAsync();
            return user;
        };

        user = new Models.User
        {
            TelegramId = telegramId,
            Language = LanguageEnum.Rus
        };
        _dbContext.Users.Add(user);
        
        await _dbContext.SaveChangesAsync();
        
        return user;
    }

    private async Task<Models.Payment> GetOrCreatePaymentAsync(int userId, decimal? amount = null)
    {
        var payment = await _dbContext.Payments.FirstOrDefaultAsync
        (p =>
            p.UserId == userId && 
            p.Status == PaymentStatusEnum.Initialized
        );

        if (payment is not null) return payment;
        
        payment = new Payment
        {
            UserId = userId,
            Amount = amount ?? 300,
            Status = PaymentStatusEnum.Initialized
        };
        
        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync();

        return payment;
    }
    private async Task<Message> Start(Message message, CancellationToken cancellationToken)
        {
            InlineKeyboardMarkup inlineKeyboard = new
            (
                new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.CreatePost, ScenarioEnum.CreatePost.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.CreatePlan, ScenarioEnum.CreatePlan.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.DownloadIgTt, ScenarioEnum.DownloadIgTt.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.DownloadPinterest, ScenarioEnum.DownloadPinterest.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.RelatedHashtags, ScenarioEnum.RelatedHashtags.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.CompetitorHashtags, ScenarioEnum.CompetitorHashtags.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.NeighboringHashtags, ScenarioEnum.NeighboringHashtags.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.HashtagsByPhoto, ScenarioEnum.HashtagsByPhoto.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.LineBreak, ScenarioEnum.LineBreak.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.VoiceMessageToText, ScenarioEnum.VoiceMessageToText.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.TextDetectionByImage, ScenarioEnum.TextDetectionByImage.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.TextToImage, ScenarioEnum.TextToImage.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.PresetsByPhoto, ScenarioEnum.PresetsByPhoto.ToString()),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData(_translate.RandomNumberGenerator, ScenarioEnum.RandomNumberGenerator.ToString()),
                    }
                }
            );
            
            return await _botClient.SendTextMessageAsync
            (
                chatId: message.Chat.Id,
                text: _translate.StartMessage,
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken
            );
        }
    private async Task Subscribe(Message message, CancellationToken cancellationToken)
    {
        var state = _states[message.Chat.Id];
        
        if (state.User.IsPayed)
        {
            await _botClient.SendTextMessageAsync
            (
                message.Chat.Id,
                $"Access open to {state.User.ConfirmedPayment!.DateConfirmed}",
                cancellationToken: cancellationToken
            );
            return;
        }

        const decimal amount = 300;
        var payment = await GetOrCreatePaymentAsync(state.User.Id, amount);
        var link = $"{QronpayBaseUrl}/Payments/CreatePayment?Amount={amount}&ServiceId=5&SellerExternalId={payment.Id}";
        
        var inlineKeyboard = new InlineKeyboardMarkup
        (
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Pay", link)
                }
            }
        );

        await _botClient.SendTextMessageAsync
        (
            message.Chat.Id,
            "Press /check_subscription after payment", 
            replyMarkup: inlineKeyboard,
            cancellationToken: cancellationToken
        );
    }
    private async Task CheckSubscription(Message message, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        var user = _states[message.Chat.Id].User = await GetOrCreateUserAsync(message.Chat.Id);
        var payment = user.InitializedPayment;

        if (payment is not null)
        {
            var link = $"{QronpayBaseUrl}/Payments/CheckPaymentStatus?PaymentId={payment.Id}&ServiceId=5";
            PaymentConfirmation? response;

            try
            {
                response = await client.GetFromJsonAsync<PaymentConfirmation>(link, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                response = null;
            }

            if (response?.PaymentStatus == "Created" && response.Result == "Success")
            {
                payment.Confirm();
                await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
                await Subscribe(message, cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Payment not confirmed", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await Subscribe(message, cancellationToken);
        }
    }
    private async Task ChangeLanguage(Message message, CancellationToken cancellationToken)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup
        (
            new[]
            {
                new[]
                {
                    new KeyboardButton("English"),
                    new KeyboardButton("Русский")
                }
            }
        )
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
                        
        await _botClient.SendTextMessageAsync
        (
            chatId: message.Chat.Id,
            text: "Choose language:",
            replyMarkup: replyKeyboardMarkup,
            cancellationToken: cancellationToken
        );
        
        var state = _states[message.From!.Id];
        state.Scenario = ScenarioEnum.ChangeLanguage;
        state.Step = "ChooseLanguage";
    }
    private async Task ScenarioHandler(Message message, CancellationToken cancellationToken)
    {
        var state = _states[message.Chat.Id];

        if (!state.User.IsPayed) 
        {
            await _botClient.SendTextMessageAsync
            (
                message.Chat.Id,
                "Buy a subscription to access the bot using command /subscribe",
                cancellationToken: cancellationToken
            );
            
            return;
        }
        
        switch (state.Scenario)
        {
            case ScenarioEnum.TextToImage:
                switch (state.Step)
                {
                    case "FillText":
                    {
                        state.Request = new TextToImageRequest(message.Chat.Id, state.User.Language)
                        {
                            Text = message.Text!
                        };

                        await _botClient.SendTextMessageAsync
                        (
                            chatId: message.Chat.Id,
                            text: _translate.SendImage,
                            cancellationToken: cancellationToken
                        );

                        state.Step = "FillImg";
                    }
                    break;
                    case "FillImg":
                    {
                        var request = (TextToImageRequest)state.Request;
                        request.FileId = message.Photo?.LastOrDefault()?.FileId!;
                        
                        _clientsService.StartScenario(state.Scenario, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.LineBreak:
                switch (state.Step)
                {
                    case "FillText":
                    {
                        state.Request = new LineBreakRequest(message.Chat.Id, state.User.Language)
                        {
                            Text = message.Text!
                        };
                        var request = (LineBreakRequest)state.Request;

                        _clientsService.StartScenario(ScenarioEnum.LineBreak, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.DownloadIgTt:
                switch (state.Step)
                {
                    case "FillLink":
                    {
                        state.Request = new DownloadIgTtRequest(message.Chat.Id, state.User.Language)
                        {
                            Link = message.Text!
                        };
                        var request = (DownloadIgTtRequest)state.Request;
                        
                        // _clientsService.StartScenario(ScenarioEnum.DownloadIgTt, request);

                        if (request.Link.Contains("instagram.com"))
                        {
                            var fileLinks = await _igService.GetDownloadLinksAsync(request.Link);
                            
                            if (fileLinks is not null) 
                            {
                                var media = new List<IAlbumInputMedia>();

                                foreach (var link in fileLinks)
                                {
                                    if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) continue;

                                    var extension = Path.GetExtension(uri.LocalPath).ToLower();

                                    switch (extension)
                                    {
                                        case ".jpg":
                                        case ".jpeg":
                                        case ".png":
                                        case ".gif":
                                        case ".webp":
                                            media.Add(new InputMediaPhoto(new InputFileUrl(link)));
                                            break;
                                        case ".mp4":
                                        case ".avi":
                                        case ".mov":
                                        case "":
                                            media.Add(new InputMediaVideo(new InputFileUrl(link)));
                                            break;
                                    }
                                }

                                await _botClient.SendMediaGroupAsync(message.Chat.Id, media, cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync
                                (
                                    message.Chat.Id, 
                                    "Not found the story",
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        else if (request.Link.Contains("tiktok.com"))
                        {
                            var stream = await _ttService.GetDownloadLinksAsync(request.Link);
                            if (stream is not null)
                            {
                                await _botClient.SendVideoAsync
                                (
                                    message.Chat.Id, 
                                    new InputFileStream(stream),
                                    cancellationToken: cancellationToken
                                );
                            }
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync
                            (
                                message.Chat.Id, 
                                "Invalid link",
                                cancellationToken: cancellationToken
                            );
                        }
                    }
                    break;
                }
                break;
            case ScenarioEnum.TextDetectionByImage:
                switch (state.Step)
                {
                    case "FillImage":
                    {
                        state.Request = new TextDetectionByImageRequest(message.Chat.Id, state.User.Language)
                        {
                            UserId = message.Chat.Id
                        };

                        var imgId = message.Photo?.LastOrDefault()?.FileId;

                        if (imgId is null) break;
                        
                        var img = await _botClient.GetFileAsync(imgId, cancellationToken);

                        var request = (TextDetectionByImageRequest)state.Request;
                        request.FileId = img.FileId;
                        await _botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);
                        _clientsService.StartScenario(state.Scenario, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.DownloadPinterest:
                switch (state.Step)
                {
                    case "FillLink":
                    {
                        state.Request = new DownloadPinterestRequest(message.Chat.Id, state.User.Language)
                        {
                            Link = message.Text!
                        };
                        var request = (DownloadPinterestRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.DownloadPinterest, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.CreatePost:
                switch (state.Step)
                {
                    case "FillTopic":
                    {
                        state.Request = new CreatePostRequest(message.Chat.Id, state.User.Language)
                        {
                            Topic = message.Text!
                        };

                        var replyKeyboardMarkup = new ReplyKeyboardMarkup
                        (
                            new[]
                            {
                                new[]
                                {
                                    new KeyboardButton(_translate.Instagram),
                                    new KeyboardButton(_translate.TgVk)
                                }
                            }
                        )
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };
                        
                        await _botClient.SendTextMessageAsync
                        (
                            message.Chat.Id,
                            _translate.ChooseForResource,
                            replyMarkup: replyKeyboardMarkup,
                            cancellationToken: cancellationToken
                        );
                        
                        state.Step = "FillFor";
                    }
                    break;
                    case "FillFor":
                    {
                        var request = (CreatePostRequest)state.Request;
                        request.For = message.Text!;

                        // var query = $"Language: {request.Language}\nContext: Create a post for {request.For}\nTopic: {request.Topic}\n\nQuery: Create text of the post on given topic, on selected language and return";
                        var query = $"Write a post for {request.For} on the topic {request.Topic}. (Language: {request.Language})";
                        var post = await _gptService.Request(query);

                        post = await _gptService.Request(new List<ChatMessage>
                        {
                            new ("system", $"You are content writer on the topic [{request.Topic}]"),
                            new ("user", $"Generate post for Instagram with emojis and hashtags on topic [{request.Topic}] and translate it to Russian"),
                            new ("assistant", $"Please generate a solid standalone text in language [{request.Language}] without excessive repetition. Response must end by semantics and other shouldn't be started. Hashtags must be at the end. Desired post length is about 750-1500 chars."),
                        });
                        
                        await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: post,
                            replyToMessageId: message.MessageId,
                            cancellationToken: cancellationToken
                        );
                    }
                    break;
                }
                break;
            case ScenarioEnum.CreatePlan:
                switch (state.Step)
                {
                    case "FillTopic":
                    {
                        state.Request = new CreatePlanRequest(message.Chat.Id, state.User.Language)
                        {
                            Topic = message.Text!
                        };
                        
                        await _botClient.SendTextMessageAsync
                        (
                            chatId: message.Chat.Id,
                            text: _translate.CreatePlanFillAudience,
                            cancellationToken: cancellationToken
                        );
                        
                        state.Step = "FillAudience";
                    }
                    break;
                    case "FillAudience":
                    {
                        var request = (CreatePlanRequest)state.Request;
                        request.Audience = message.Text!;
                        
                        // _clientsService.StartScenario(ScenarioEnum.CreatePlan, request);

                        var replyKeyboardMarkup = new ReplyKeyboardMarkup
                        (
                            new[]
                            {
                                new[]
                                {
                                    new KeyboardButton(_translate.Week),
                                    new KeyboardButton(_translate.Month),
                                    new KeyboardButton(_translate.Yeah)
                                }
                            }
                        )
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };
                        
                        await _botClient.SendTextMessageAsync
                        (
                            message.Chat.Id,
                            _translate.CreatePlanFillPeriod,
                            replyMarkup: replyKeyboardMarkup,
                            cancellationToken: cancellationToken
                        );
                        
                        state.Step = "FillPeriod";
                    }
                    break;
                    case "FillPeriod":
                    {
                        var request = (CreatePlanRequest)state.Request;
                        request.Period = message.Text!;

                        var plan = await _gptService.Request(new List<ChatMessage>
                        {
                            new ChatMessage("system", $"You are SMM specialist, writing content-plan on topic [{request.Topic}] for period [{request.Period}]. Your potential clients are in general [{request.Audience}]"),
                            new ChatMessage("user", $"Generate content plan for [{request.Audience}] on topic [{request.Topic}] for period [{request.Period}] and translate it to Russian"),
                            new ChatMessage("assistant", $"Please generate a solid standalone text in language [Russian] without excessive repetition. Response must end by semantics and other shouldn't be started. Hashtags must be at the end. Desired post length is about 750-1500 chars.")
                        });

                        await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: plan,
                            replyToMessageId: message.MessageId,
                            cancellationToken: cancellationToken
                        );
                    }
                    break;
                }
                break;
            case ScenarioEnum.RelatedHashtags:
                switch (state.Step)
                {
                    case "FillHashtag":
                    {
                        state.Request = new RelatedHashtagsRequest(message.Chat.Id, state.User.Language)
                        {
                            Hashtag = message.Text!
                        };
                        var request = (RelatedHashtagsRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.RelatedHashtags, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.VoiceMessageToText:
                switch (state.Step)
                {
                    case "FillVoice":
                    {
                        state.Request = new VoiceToTextRequest(message.Chat.Id, state.User.Language)
                        {
                            Voice = message.Voice!
                        };
                        var request = (VoiceToTextRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.VoiceMessageToText, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.CompetitorHashtags:
                switch (state.Step)
                {
                    case "FillUsername":
                    {
                        state.Request = new CompetitorHashtagsRequest(message.Chat.Id, state.User.Language)
                        {
                            Username = message.Text!
                        };
                        var request = (CompetitorHashtagsRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.CompetitorHashtags, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.NeighboringHashtags:
                switch (state.Step)
                {
                    case "FillHashtag":
                    {
                        state.Request = new NeighboringHashtagsRequest(message.Chat.Id, state.User.Language)
                        {
                            Hashtag = message.Text!
                        };
                        var request = (NeighboringHashtagsRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.NeighboringHashtags, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.HashtagsByPhoto:
                switch (state.Step)
                {
                    case "FillPhoto":
                    {
                        state.Request = new HashtagsByPhotoRequest(message.Chat.Id, state.User.Language)
                        {
                            FileId = message.Photo?.LastOrDefault()?.FileId!
                        };
                        var request = (HashtagsByPhotoRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.HashtagsByPhoto, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.RandomNumberGenerator:
                switch (state.Step)
                {
                    case "FillRange":
                    {
                        var regex = new Regex(@"(\d+)\s*-\s*(\d+)");
                        var match = regex.Match(message.Text!);

                        if (!match.Success) break;

                        var from = int.Parse(match.Groups[1].Value);
                        var to = int.Parse(match.Groups[2].Value);

                        if(from > to) break;

                        await _botClient.SendTextMessageAsync
                        (
                            message.Chat.Id, 
                            $"Random number: {new Random().Next(from, to + 1)}", 
                            cancellationToken: cancellationToken
                        );
                    }
                    break;
                }
                break;
            case ScenarioEnum.PresetsByPhoto:
                switch (state.Step)
                {
                    case "FillPhoto":
                    {
                        state.Request = new PresetsByPhotoRequest(message.Chat.Id, state.User.Language)
                        {
                            FileId = message.Photo?.LastOrDefault()?.FileId!
                        };

                        var request = (PresetsByPhotoRequest)state.Request;
                        
                        _clientsService.StartScenario(ScenarioEnum.PresetsByPhoto, request);
                    }
                    break;
                }
                break;
            case ScenarioEnum.ChangeLanguage:
                switch (state.Step)
                {
                    case "ChooseLanguage":
                    {
                        switch (message.Text)
                        {
                            case "English":
                                state.User.Language = LanguageEnum.Eng;
                                break;
                            case "Русский":
                                state.User.Language = LanguageEnum.Rus;
                                break;
                        }
                        
                        _translate.Language = state.User.Language;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        
                        await Start(message, cancellationToken);
                    }
                    break;
                }
                break;
        }
    }
}
