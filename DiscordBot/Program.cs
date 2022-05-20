using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.HostedServices;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VkNet;
using VkNet.AudioBypassService.Extensions;
using YoutubeExplode;

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
            x.GetRequiredService<AudioGuildManager>(),
            x.GetRequiredService<InteractionService>()));
    }).Build();

using (host)
{
    await host.RunAsync();
}
