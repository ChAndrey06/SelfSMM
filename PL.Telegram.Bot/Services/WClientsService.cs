using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PL.Telegram.Bot.Enums;
using PL.Telegram.Bot.Models;
using PL.Telegram.Bot.Models.Requests;
using Telegram.Bot;

namespace PL.Telegram.Bot.Services;

public class WClientsService : BackgroundService
{
    public static WClientsService? Instance = null;
    
    private List<WClientContext> _contexts;
    private readonly AppDbContext _dbContext;
    private readonly ITelegramBotClient _botClient;
    
    public WClientsService
    (
        IOptions<List<ClientConfig>> clientConfigs,
        ITelegramBotClient botClient,
        AppDbContext dbContext
    )
    {
        Instance = this;
        _dbContext = dbContext;
        _botClient = botClient;
        _contexts = new List<WClientContext>();
    }

    private async Task<WClientContext> GetFreeContext(long? id = null)
    {
        while (true)
        {
            var context = _contexts
                .Where(c => c.Status.Equals(ClientStatusEnum.Free) && (id is null || c.Config.Id.Equals(id.Value)))
                .MinBy(c => c.SessionStarted);
            
            if (context is not null)
            {
                context.Busy();
                return context;
            }
            await Task.Delay(10);
        }
    }

    public async Task OnBanned(WClientContext context)
    {
        context.Config.Banned = true;
        await _dbContext.SaveChangesAsync();
        
        _contexts.Remove(context);
    }
    
    public void StartScenario(ScenarioEnum scenario, BaseRequest request)
    {
        Task.Run(async () =>
        {
            var freeContext = await GetFreeContext();
            await freeContext.StartScenario(scenario, request);
        });
    }
    public void StartInterruptibleScenario(ScenarioEnum scenario, InterruptibleRequest request)
    {
        Task.Run(async () =>
        {
            var messageMatcher = await _dbContext.Messages.FirstOrDefaultAsync(m => m.ChildBotMessageId.Equals(request.CallbackQuery.Message!.MessageId));

            if (messageMatcher is null) return;
            
            var freeContext = await GetFreeContext(messageMatcher.ParentBotId);
            freeContext.MessageMatcher = messageMatcher;
            await freeContext.StartScenario(scenario, request);
        });
    }
    
    public override void Dispose() 
    {
        _contexts.ForEach(client => client.Client.Dispose());
        
        base.Dispose();
    }
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var clientConfigs = await _dbContext.ClientConfigs
            .Where(c => !c.Banned && c.InUse)
            .ToListAsync(cancellationToken: cancellationToken);
        
        _contexts = clientConfigs.Select(config => new WClientContext(config, _botClient, _dbContext, this)).ToList();
        
        await Task.WhenAll(_contexts.Select(client => client.Activate()));
    }
}