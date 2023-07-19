using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using PL.Telegram.Bot.Extensions;
using PL.Telegram.Bot.Models;
using PL.Telegram.Bot.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Configuration));
        services.Configure<List<ClientConfig>>(context.Configuration.GetSection("ClientConfigs"));
        services.Configure<GptConfig>(context.Configuration.GetSection("GptConfig"));
        services.Configure<List<string>>(context.Configuration.GetSection("IgApi"));
        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                var botConfig = sp.GetConfiguration<BotConfiguration>();
                TelegramBotClientOptions options = new(botConfig.BotToken);

                return new TelegramBotClient(options, httpClient);
            });

        // services.AddDbContext<AppDbContext>(options => options.UseNpgsql(context.Configuration.GetConnectionString("AppDbContext")));
        
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("AppDbContext");
            options.UseNpgsql(connectionString);
        }, ServiceLifetime.Singleton);
        
        services.AddHostedService<WClientsService>();
        services.AddSingleton<WClientsService>(provider =>
        {
            if (WClientsService.Instance is not null)
                return WClientsService.Instance;
            
            var clientConfigs = provider.GetRequiredService<IOptions<List<ClientConfig>>>();
            var botClient = provider.GetRequiredService<ITelegramBotClient>();
            var dbContext = provider.GetRequiredService<AppDbContext>();
                
            return new WClientsService(clientConfigs, botClient, dbContext);
        });
        
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
        services.AddTransient<ChatGptService>();
        services.AddTransient<DownloadInstagramService>();
        services.AddTransient<DownloadIgApiService>();
        services.AddTransient<DownloadTiktokService>();

        HttpClientFactory.Proxies = context.Configuration.GetSection("Proxy").Get<ProxyConfiguration[]>();

    })
    .UseWindowsService()
    .Build();

await host.RunAsync();

public class BotConfiguration
{
    public static readonly string Configuration = "BotConfiguration";
    public string BotToken { get; set; } = "";
}