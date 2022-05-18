using Discord.WebSocket;
using Discord;
using DiscordBot;
using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using VkNet.AudioBypassService.Extensions;
using YoutubeExplode;
using Microsoft.Extensions.Configuration;
using VkNet;
using VkNet.Model;
using VkNet.Enums.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Discord.Addons.Hosting;
using DiscordBot.HostedServices;

//public class Program
//{
//    public static void Main()
//    {
//        new Program().MainAsync().GetAwaiter().GetResult();
//    }

//    private DiscordSocketClient _client;
//    private InteractionService _commands;
//    private IConfiguration _config;
//    private Events _events;
//    private ulong _guildId;

//    public async Task MainAsync()
//    {
//        var builder = new ConfigurationBuilder()
//            .SetBasePath(AppContext.BaseDirectory)
//            .AddJsonFile(path: "appsettings.json");
//        _config = builder.Build();
//        _guildId = ulong.Parse(_config["GuildId"]);
//        var services = BuildServiceProvider();
//        _events = new(services.GetRequiredService<Storage>(),
//            services.GetRequiredService<MusicService>(),
//            services.GetRequiredService<AudioGuildManager>());

//        try
//        {
//            var client = services.GetRequiredService<DiscordSocketClient>();
//            var commands = services.GetRequiredService<InteractionService>();
//            var vk = services.GetRequiredService<VkApi>();
//            _client = client;
//            _commands = commands;

//            try
//            {
//                var login = _config["VkLogin"];
//                var password = _config["VkPassword"];
//                await vk.AuthorizeAsync(new ApiAuthParams
//                {
//                    ApplicationId = 1998,
//                    TwoFactorSupported = true,
//                    Login = login,
//                    Password = password,
//                    Settings = Settings.All,
//                    TwoFactorAuthorization = () =>
//                    {
//                        Console.WriteLine("For Vk we need your code, you have 2 minutes to enter it");
//                        return Console.ReadLine();
//                    }
//                });
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.ToString());
//                var storage = services.GetRequiredService<Storage>();
//                storage.IsVkEnabled = false;
//            }


//            _client.Log += _events.Log;
//            _commands.Log += _events.Log;
//            _client.Ready += ReadyAsync;
//            _client.SelectMenuExecuted += _events.CatchSelectOption;

//            var token = _config["token"];

//            await _client.LoginAsync(TokenType.Bot, token);
//            await _client.StartAsync();

//            await services.GetRequiredService<CommandHandler>().InitializeAsync();

//            await Task.Delay(-1);
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e.ToString());
//        }
//        finally
//        {

//        }
//    }

//    private async Task ReadyAsync()
//    {
//        _ = await _commands.RegisterCommandsGloballyAsync();
//        //await _commands.RegisterCommandsToGuildAsync(_guildId);
//    }

//    private IServiceProvider BuildServiceProvider()
//    {
//        return new ServiceCollection()
//            .AddSingleton<DiscordSocketClient>()
//            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
//            .AddSingleton<CommandHandler>()
//            .AddSingleton(x => new VkApi(new ServiceCollection().AddAudioBypass()))
//            .AddSingleton<YoutubeClient>()
//            .AddSingleton<Storage>()
//            .AddSingleton(x => new Youtube(_config))
//            .AddSingleton(x =>
//                new MusicService(x.GetRequiredService<Storage>(),
//                                 x.GetRequiredService<YoutubeClient>()))
//            .AddSingleton<AudioGuildManager>()
//            .BuildServiceProvider();
//    }
//}

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(x =>
    {
        var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", false, true)
                        .Build();

        x.AddConfiguration(configuration);
    })
    .ConfigureDiscordHost((context, config) =>
    {
        config.SocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Verbose,
            AlwaysDownloadUsers = true,
            MessageCacheSize = 200
        };

        config.Token = context.Configuration["token"];
    })
    .UseInteractionService((context, config) =>
    {
        config.LogLevel = LogSeverity.Info;
        config.UseCompiledLambda = true;
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<CommandHandler>();
        services.AddHostedService<BotStatusService>();
        services.AddSingleton<VkApiService>();
        services.AddSingleton(x => new VkApi(
            new ServiceCollection()
                .AddAudioBypass()));
        services.AddSingleton<YoutubeClient>();
        services.AddSingleton<Storage>();
        services.AddSingleton(x => new MusicService(
            x.GetRequiredService<Storage>(),
            x.GetRequiredService<YoutubeClient>()));
        services.AddSingleton<AudioGuildManager>();
        services.AddSingleton(x => new Events(
            x.GetRequiredService<Storage>(),
            x.GetRequiredService<MusicService>(),
            x.GetRequiredService<AudioGuildManager>(),
            x.GetRequiredService<InteractionService>()));
    }).Build();

using (host)
{
    await host.RunAsync();
}
