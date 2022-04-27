using Discord.WebSocket;
using Discord;
using DiscordBot;
using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using VkNet.AudioBypassService.Extensions;
using YoutubeExplode;

public class Program
{
    public static void Main(string[] args) =>
        new Program().MainAsync().GetAwaiter().GetResult();

    private DiscordSocketClient _client;
    private InteractionService _commands;
    private Events _events = new();
    private ulong _guildId; 

    public async Task MainAsync()
    {
        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GuildId"));
        var services = BuildServiceProvider();
        try
        {
            var client = services.GetRequiredService<DiscordSocketClient>();
            var commands = services.GetRequiredService<InteractionService>();
            _client = client;
            _commands = commands;

            _client.Log += _events.Log;
            _commands.Log += _events.Log;
            _client.Ready += ReadyAsync;

            var token = Environment.GetEnvironmentVariable("token");

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

    private IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .AddAudioBypass()
            .AddSingleton<YoutubeClient>()
            .BuildServiceProvider();
}
