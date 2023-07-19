using Microsoft.EntityFrameworkCore;
using PL.Telegram.Bot.Enums;
using PL.Telegram.Bot.Models;
using PL.Telegram.Bot.Models.Requests;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TL;
using BotMessage = Telegram.Bot.Types.Message;
using BotInputMediaPhoto = Telegram.Bot.Types.InputMediaPhoto;
using BotInputMediaDocument = Telegram.Bot.Types.InputMediaDocument;
using Document = TL.Document;
using DocumentAttributeAudio = TL.DocumentAttributeAudio;
using File = System.IO.File;
using Message = TL.Message;

namespace PL.Telegram.Bot.Services;

public class WClientContext
{
    public WTelegram.Client Client { get; }
    public ClientStatusEnum Status { get; set; }
    public ClientConfig Config { get; set; }
    public Models.Message MessageMatcher { get; set; }
    public Guid SessionId { get; set; }
    public DateTime SessionStarted { get; set; }
    
    private InputPeer _peer;
    private UserState _state;
    private LanguageEnum _language;
    private readonly WClientsService _clientsService;
    private readonly TranslateService _translate;
    private readonly ITelegramBotClient _botClient;
    private readonly AppDbContext _dbContext;
    
    public WClientContext
    (
        ClientConfig config,
        ITelegramBotClient botClient,
        AppDbContext dbContext,
        WClientsService clientsService
    ) 
    {
        Config = config;
        _botClient = botClient;
        _dbContext = dbContext;
        _clientsService = clientsService;
        _translate = new TranslateService();
        _language = LanguageEnum.Rus;
        _state = new UserState();

        Status = ClientStatusEnum.None;
        Client = new WTelegram.Client(what =>
        {
            switch (what)
            {
                case "api_id": return config.ApiId;
                case "api_hash": return config.ApiHash;
                case "phone_number": return config.PhoneNumber;
                case "verification_code": Console.WriteLine($"Code for number '{config.PhoneNumber}': "); return Console.ReadLine();
                case "first_name": return config.FirstName;
                case "last_name": return config.LastName;
                case "password": return config.Password;
                case "session_pathname": return $"Sessions/{config.Password}";
                default: return null;
            }
        });
    }

    public async Task Activate() 
    {
        try
        {
            await Client.Login(Config.PhoneNumber);
        }
        catch (RpcException e)
        {
            switch (e.Message)
            {
                case "PHONE_NUMBER_BANNED":
                    await _clientsService.OnBanned(this);
                    break;
            }
            return;
        }
        
        _peer = await SchemaExtensions.Contacts_ResolveUsername(Client, "SelfSMMBot");

        Client.OnUpdate += OnUpdate;
        
        Status = ClientStatusEnum.Free;
    }

    public void Free() 
    {
        Status = ClientStatusEnum.Free;
    }
    public void Busy() 
    {
        Status = ClientStatusEnum.Busy;
    }
    
    private async Task OnUpdate(UpdatesBase updates)
    {
        var handlers = new List<Task>();

        foreach (var update in updates.UpdateList)
            switch (update)
            {
                case UpdateNewMessage unm: 
                    handlers.Add(OnNewMessage(unm));
                    break;
                case UpdateEditMessage unm:
                    handlers.Add(OnEditMessage(unm));
                    break;
            }

        await Task.WhenAll(handlers);
    }

    private async Task OnNewMessage(UpdateNewMessage message) 
    {
        if (message.message.Peer.ID == _peer.ID && message.message?.From?.ID != Config.Id)
        {
            await ScenarioHandler(message.message);
        }
    }
    private async Task OnEditMessage(UpdateEditMessage message) 
    {
        if (message.message.Peer.ID != _peer.ID || message.message?.From?.ID == Config.Id)
            return;
        
        var messageBase = message.message;

        var messageMatcher = await GetMessageMap(messageBase.ID);

        if (messageMatcher is not null && (_state.Scenario == ScenarioEnum.None || Guid.NewGuid() != SessionId))
        {
            await EditChildMessage(messageBase, messageMatcher.ClientTelegramId, messageMatcher.ChildBotMessageId);
            
            return;
        }
        
        switch (_state.Scenario)
        {
            case ScenarioEnum.Interruptible:
            {
                switch (_state.Step)
                {
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();
                        
                        var request = _state.Request as InterruptibleRequest;
                        await EditChildMessage(messageBase, request.UserId, MessageMatcher.ChildBotMessageId);
                    }
                    break;
                }
            }
            break;
            default:
                await ScenarioHandler(message.message);
                break;
        }
    }
    
    public async Task StartScenario(ScenarioEnum scenario, BaseRequest request)
    {
        SessionId = Guid.NewGuid();
        SessionStarted = DateTime.Now;
        _state = new UserState
        {
            Scenario = scenario,
            Request = request,
            Step = ""
        };

        _language = request.Language;
        await ScenarioHandler();
    }

    private async Task ScenarioHandler(MessageBase? messageBase = null)
    {
        if (_translate.Language != _language)
        {
            switch (messageBase)
            {
                case null:
                    await WithDelay(() => SendMessageAsync("/change_lang"));
                    break;
                case Message msg:
                {
                    await WithDelay(() => PressBtnAsync(msg, _language == LanguageEnum.Eng ? "🇺🇸" : "🇷🇺", true));
        
                    _translate.Language = _language;
                }
                break;
            }
            
            return;
        }
        
        switch (_state.Scenario) 
        {
            case ScenarioEnum.TextToImage:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendPhoto";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.TextToImage));
                        break;
                    case "SendPhoto":
                    {
                        _state.Step = "WaitResult";
                        var request = (TextToImageRequest)_state.Request;
                        
                        var filename = $"{request.FileId}.png";
                        var voiceMessage = await _botClient.GetFileAsync(request.FileId);

                        await using var fileStream = File.Create(filename);

                        await _botClient.DownloadFileAsync(voiceMessage.FilePath!, fileStream);
                        fileStream.Close();
                        
                        var inputFile = await Client.UploadFileAsync(filename);
                        await WithDelay(() => SendMediaAsync(request.Text, inputFile));
                        
                        File.Delete(filename);
                        
                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.DownloadIgTt:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendLink";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.DownloadIgTt));
                        break;
                    case "SendLink":
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((DownloadIgTtRequest)_state.Request).Link));
                        break;
                    case "WaitResult":
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;

                        Free();
                        
                        if (messageBase is not Message msg) break;
                        
                        // TODO Refactor code repeating 
                        if (msg.media is MessageMediaPhoto { photo: Photo photo })
                        {
                            var filename = photo.id.ToString();
                            await using var fileStream = File.Create(filename);
                            await WithDelay(() => Client.DownloadFileAsync(photo, fileStream));
                            fileStream.Close();
                        
                            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await _botClient.SendPhotoAsync
                            (
                                chatId: _state.Request.UserId,
                                photo: new InputFileStream(sendFileStream, filename)
                            );
                            sendFileStream.Close();
                        
                            File.Delete(filename);
                        }
                        else if (msg.media is MessageMediaDocument { document: Document document })
                        {
                            var filename = document.Filename;
                            await using var fileStream = File.Create(filename);
                            await WithDelay(() => Client.DownloadFileAsync(document, fileStream));
                            fileStream.Close();
                        
                            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await _botClient.SendDocumentAsync
                            (
                                chatId: _state.Request.UserId,
                                document: new InputFileStream(sendFileStream, filename)
                            );
                            sendFileStream.Close();
                        
                            File.Delete(filename);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(_state.Request.UserId, string.IsNullOrEmpty(msg.message) ? _translate.ServerError : msg.message);
                        }
                        
                        break;
                }
                break;
            case ScenarioEnum.TextDetectionByImage:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendPhoto";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.TextDetectionByImage));
                        break;
                    case "SendPhoto":
                    {
                        _state.Step = "WaitResult";
                        var request = (TextDetectionByImageRequest)_state.Request;
                        
                        var filename = $"{request.FileId}.png";
                        var photo = await _botClient.GetFileAsync(request.FileId);

                        await using var fileStream = File.Create(filename);

                        await _botClient.DownloadFileAsync(photo.FilePath!, fileStream);
                        fileStream.Close();
                        
                        var inputFile = await Client.UploadFileAsync(filename);
                        await WithDelay(() => SendMediaAsync(null, inputFile));
                        
                        File.Delete(filename);
                    }
                    break;
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;  
            case ScenarioEnum.DownloadPinterest:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendLink";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.DownloadPinterest));
                        break;
                    case "SendLink":
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((DownloadPinterestRequest)_state.Request).Link));
                        break;
                    case "WaitResult":
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;

                        Free();
                        
                        if (messageBase is not Message msg) break;
                        
                        // TODO Refactor code repeating 
                        if (msg.media is MessageMediaPhoto { photo: Photo photo })
                        {
                            var filename = photo.id.ToString();
                            await using var fileStream = File.Create(filename);
                            await WithDelay(() => Client.DownloadFileAsync(photo, fileStream));
                            fileStream.Close();
                        
                            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await _botClient.SendPhotoAsync
                            (
                                chatId: _state.Request.UserId,
                                photo: new InputFileStream(sendFileStream, filename)
                            );
                            sendFileStream.Close();
                        
                            File.Delete(filename);
                        }
                        else if (msg.media is MessageMediaDocument { document: Document document })
                        {
                            var filename = document.Filename;
                            await using var fileStream = File.Create(filename);
                            await WithDelay(() => Client.DownloadFileAsync(document, fileStream));
                            fileStream.Close();
                        
                            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await _botClient.SendDocumentAsync
                            (
                                chatId: _state.Request.UserId,
                                document: new InputFileStream(sendFileStream, filename)
                            );
                            sendFileStream.Close();
                        
                            File.Delete(filename);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(_state.Request.UserId, string.IsNullOrEmpty(msg.message) ? _translate.ServerError : msg.message);
                        }
                        
                        break;
                }
                break;
            case ScenarioEnum.CreatePost:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "SendTopic";
                        await WithDelay(() => SendMessageAsync("/create_post"));
                        break;
                    case "SendTopic":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((CreatePostRequest)_state.Request).Topic));
                    }            
                    break;
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();
                        
                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;
                }
                break;
            case ScenarioEnum.CreatePlan:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "SendTopic";
                        await WithDelay(() => SendMessageAsync("/create_plan"));
                        break;
                    case "SendTopic":
                    {
                        _state.Step = "SendAudience";
                        await WithDelay(() => SendMessageAsync(((CreatePlanRequest)_state.Request).Topic));
                        
                        break;
                    }  
                    case "SendAudience":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((CreatePlanRequest)_state.Request).Audience));
                        
                        break;
                    }  
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();
                        
                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;
                    // case "WaitResult":
                    // {
                    //     if (messageBase is not Message msg) break;
                    //     if (msg.message.Equals(_translate.PleaseWait)) break;
                    //     
                    //     _state.Step = "";
                    //     _state.Scenario = ScenarioEnum.None;
                    //     
                    //     Free();
                    //
                    //     await _botClient.SendTextMessageAsync(_state.Request.UserId, msg.message);
                    //     
                    //     break;   
                    // }
                }
                break;
            case ScenarioEnum.RelatedHashtags:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "FillHashtag";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.RelatedHashtags));
                        break;
                    case "FillHashtag":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((RelatedHashtagsRequest)_state.Request).Hashtag));
                        
                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.VoiceMessageToText:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendVoice";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.VoiceMessageToText));
                        break;
                    case "SendVoice":
                    {
                        _state.Step = "WaitResult";
                        var request = (VoiceToTextRequest)_state.Request;
                        
                        var filename = $"{request.Voice.FileId}.ogg";
                        var voiceMessage = await _botClient.GetFileAsync(request.Voice.FileId);

                        await using var fileStream = File.Create(filename);

                        await _botClient.DownloadFileAsync(voiceMessage.FilePath!, fileStream);
                        fileStream.Close();
                        
                        var inputFile = await Client.UploadFileAsync(filename);

                        await WithDelay(() => Client.SendMessageAsync
                        (
                            _peer, 
                            null, 
                            new InputMediaUploadedDocument
                            {
                                file = inputFile,
                                mime_type = "audio/ogg",
                                attributes = new []
                                {
                                    new DocumentAttributeAudio
                                    {
                                        flags = DocumentAttributeAudio.Flags.voice
                                    }
                                }
                            }
                        ));
                        
                        File.Delete(filename);
                    }
                    break;
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        
                        Free();
                        
                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                        
                        break;
                    }
                }
                break;
            case ScenarioEnum.CompetitorHashtags:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "FillUsername";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.CompetitorHashtags));
                        break;
                    case "FillUsername":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((CompetitorHashtagsRequest)_state.Request).Username));
                        
                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.NeighboringHashtags:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "FillHashtag";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.NeighboringHashtags));
                        break;
                    case "FillHashtag":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((NeighboringHashtagsRequest)_state.Request).Hashtag));
                        
                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.HashtagsByPhoto:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendPhoto";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.HashtagsByPhoto));
                        break;
                    case "SendPhoto":
                    {
                        _state.Step = "WaitResult";
                        var request = (HashtagsByPhotoRequest)_state.Request;
                        
                        var filename = $"{request.FileId}.png";
                        var photo = await _botClient.GetFileAsync(request.FileId);

                        await using var fileStream = File.Create(filename);

                        await _botClient.DownloadFileAsync(photo.FilePath!, fileStream);
                        fileStream.Close();
                        
                        var inputFile = await Client.UploadFileAsync(filename);
                        await WithDelay(() => SendMediaAsync("", inputFile));
                        
                        File.Delete(filename);

                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.LineBreak:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "FillText";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.LineBreak));
                        break;
                    case "FillText":
                    {
                        _state.Step = "WaitResult";
                        await WithDelay(() => SendMessageAsync(((LineBreakRequest)_state.Request).Text));
                        
                        break;
                    }
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.PresetsByPhoto:
                switch (_state.Step)
                {
                    case "":
                        _state.Step = "PressBtn";
                        await WithDelay(() => SendMessageAsync("/start"));
                        break;
                    case "PressBtn":
                        _state.Step = "SendPhoto";
                        await WithDelay(() => PressBtnAsync(messageBase, _translate.PresetsByPhoto));
                        break;
                    case "SendPhoto":
                    {
                        _state.Step = "WaitResult";
                        var request = (PresetsByPhotoRequest)_state.Request;
                        
                        var filename = $"{request.FileId}.png";
                        var photo = await _botClient.GetFileAsync(request.FileId);

                        await using var fileStream = File.Create(filename);

                        await _botClient.DownloadFileAsync(photo.FilePath!, fileStream);
                        fileStream.Close();
                        
                        var inputFile = await Client.UploadFileAsync(filename);
                        await WithDelay(() => SendMediaAsync(null, inputFile));
                        
                        File.Delete(filename);
                    }
                    break;
                    case "WaitResult":
                    {
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();

                        await ResendToChildAsync(messageBase!, _state.Request.UserId);
                    }
                    break;   
                }
                break;
            case ScenarioEnum.Interruptible:
            {
                switch (_state.Step)
                {
                    case "":
                    {
                        _state.Step = "WaitResult";
                        var request = _state.Request as InterruptibleRequest;
                
                        var msgId = new InputMessageID
                        {
                            id = MessageMatcher.ParentBotMessageId
                        };
                
                        await Client.Messages_GetMessages(msgId);
                
                        PressBtnAsync((await Client.Messages_GetMessages(msgId)).Messages.First(), request.CallbackQuery.Data);
                    }
                    break;
                    case "WaitResult":
                    {
                        if (messageBase is Message message && message.message.Equals(_translate.PleaseWait)) break;
                        
                        _state.Step = "";
                        _state.Scenario = ScenarioEnum.None;
                        Free();
                        
                        var request = _state.Request as InterruptibleRequest;
                        await ResendToChildAsync(messageBase, request.UserId);
                    }
                    break;
                }
            }       
            break;
        }
    }
    
    private async Task PressBtnAsync(MessageBase? msgBase, string btnName, bool contains = false) 
    {
        if (msgBase is not Message { reply_markup: ReplyInlineMarkup replyInlineMarkup } msg)
            return;
        
        var btn = replyInlineMarkup.rows.SelectMany(r => r.buttons).FirstOrDefault(b => b.Text.Equals(btnName) || (contains && b.Text.Contains(btnName)));
        
        if (btn is KeyboardButtonCallback btnCallback)
        {
            await LogAction("PressBtnAsync");
            Client.Messages_GetBotCallbackAnswer(_peer, msg.id, btnCallback.data);
        }
    }
    private async Task<Message> SendMessageAsync(string? text = null, TL.InputMedia? media = null)
    {
        await LogAction("SendMessageAsync");
        return await Client.SendMessageAsync(_peer, text ?? "", media);
    }

    private async Task SendMediaAsync(string? caption, InputFileBase mediaFile)
    {
        await LogAction("SendMediaAsync");
        await Client.SendMediaAsync(_peer, caption, mediaFile);
    }
    
    private async Task ResendToChildAsync(MessageBase msgBase, long chatId)
    {
        if (msgBase is not Message message) return;

        BotMessage sentMessage;
        var markup = MarkupToChildMarkup(message.reply_markup);
        
        if (msgBase is Message { media: MessageMediaPhoto  { photo: Photo photo } })
        {
            var filename = photo.id.ToString();
            await using var fileStream = File.Create(filename);
            await Client.DownloadFileAsync(photo, fileStream);
            fileStream.Close();
            
            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            sentMessage = await _botClient.SendPhotoAsync
            (
                chatId: _state.Request.UserId,
                photo: new InputFileStream(sendFileStream, filename),
                caption: message.message,
                replyMarkup: markup
            );
            sendFileStream.Close();
            
            File.Delete(filename);
        }
        else
        {
            sentMessage = await _botClient.SendTextMessageAsync
            (
                chatId, 
                message.message,
                replyMarkup: markup
            );
        }

        _dbContext.Messages.Add(new Models.Message
        {
            ClientTelegramId = _state.Request.UserId,
            ChildBotMessageId = sentMessage.MessageId,
            ParentBotMessageId = message.ID,
            ParentBotId = Config.Id
        });

        await _dbContext.SaveChangesAsync();
    }
    
    private async Task EditChildMessage(MessageBase msgBase, long chatId, int childMsgId)
    {
        if (msgBase is not Message message) return;
        
        var markup = MarkupToChildMarkup(message.reply_markup);
        
        if (msgBase is Message { media: MessageMediaPhoto  { photo: Photo photo } })
        {
            var filename = photo.id.ToString();
            await using var fileStream = File.Create(filename);
            await Client.DownloadFileAsync(photo, fileStream);
            fileStream.Close();
            
            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            var newPhoto = new BotInputMediaPhoto(new InputFileStream(sendFileStream, filename))
            {
                Caption = message.message
            };
            
            await _botClient.EditMessageMediaAsync
            (
                chatId: _state.Request.UserId,
                messageId: childMsgId,
                media: newPhoto,
                replyMarkup: markup
            );
            sendFileStream.Close();
            
            File.Delete(filename);
        }
        else if (msgBase is Message { media: MessageMediaDocument { document: Document document } })
        {
            var filename = document.id.ToString();
            await using var fileStream = File.Create(filename);
            await Client.DownloadFileAsync(document, fileStream);
            fileStream.Close();

            await using var sendFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            var newDocument = new BotInputMediaDocument(new InputFileStream(sendFileStream, document.Filename))
            {
                Caption = message.message
            };

            await _botClient.EditMessageMediaAsync
            (
                chatId: _state.Request.UserId,
                messageId: childMsgId,
                media: newDocument,
                replyMarkup: markup
            );
            sendFileStream.Close();

            File.Delete(filename);
        }
        else
        {
            await _botClient.EditMessageTextAsync
            (
                chatId, 
                childMsgId,
                message.message,
                replyMarkup: markup
            );
        }
    }
    
    private InlineKeyboardMarkup? MarkupToChildMarkup(ReplyMarkup replyMarkup)
    {
        if (replyMarkup is not ReplyInlineMarkup replyInlineMarkup)
            return null;

        var inlineKeyboardRows = replyInlineMarkup.rows
            .Select(row => row.buttons
                .Select(button =>
                {
                    var inlineKeyboardButton = new InlineKeyboardButton(button.Text);

                    // if (button is KeyboardButtonCallback btnCallback)
                    //     inlineKeyboardButton.CallbackData = btnCallback.data.ToString();

                    inlineKeyboardButton.CallbackData = button.Text;

                    return inlineKeyboardButton;
                })
                .ToList())
            .ToList();

        return new InlineKeyboardMarkup(inlineKeyboardRows);
    }
    
    private async Task WithDelay(Func<Task> func) 
    {
        await Task.Delay(new Random().Next(3000, 6000));
        await func();
    }
    
    private async Task WithDelay(Action func) 
    {
        await Task.Delay(new Random().Next(3000, 6000));
        func();
    }

    private async Task LogAction(string action) 
    {
        _dbContext.ClientActionsLogs.Add(new ClientActionsLog
        {
            Action = action,
            ClientId = Config.Id,
            Scenario = _state.Scenario,
            SessionId = SessionId
        });
        await _dbContext.SaveChangesAsync();
    }

    private async Task<Models.Message?> GetMessageMap(int parentBotMessageId)
    {
        return await _dbContext.Messages.FirstOrDefaultAsync(m => m.ParentBotMessageId == parentBotMessageId);
    }
}