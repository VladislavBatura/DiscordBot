using Discord.WebSocket;
using Discord;
using DiscordBot;
using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using VkNet.AudioBypassService.Extensions;
using YoutubeExplode;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static void Main()
    {
        new Program().MainAsync().GetAwaiter().GetResult();
    }

    private DiscordSocketClient _client;
    private InteractionService _commands;
    private IConfiguration _config;
    private Events _events;
    private ulong _guildId;

    public async Task MainAsync()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(path: "appsettings.json");
        _config = builder.Build();
        _guildId = ulong.Parse(_config["GuildId"]);
        var services = BuildServiceProvider();
        _events = new(services.GetRequiredService<Storage>());

        try
        {
            var client = services.GetRequiredService<DiscordSocketClient>();
            var commands = services.GetRequiredService<InteractionService>();
            _client = client;
            _commands = commands;

            _client.Log += _events.Log;
            _commands.Log += _events.Log;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += _events.CatchMessage;

            var token = _config["token"];

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            
        }
    }

    private async Task ReadyAsync()
    {
        await _commands.RegisterCommandsToGuildAsync(_guildId);
    }

    private IServiceProvider BuildServiceProvider()
    {
        return new ServiceCollection()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .AddAudioBypass()
            .AddSingleton<YoutubeClient>()
            .AddSingleton<Storage>()
            .AddSingleton(x => new Youtube(_config))
            .AddSingleton(x =>
                new MusicService(x.GetRequiredService<Storage>(),
                                 x.GetRequiredService<YoutubeClient>()))
            .BuildServiceProvider();
    }
}
