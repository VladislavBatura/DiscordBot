using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBot
{
    public class BotStatusService : DiscordClientService
    {
        public BotStatusService(DiscordSocketClient client,
                                ILogger<DiscordClientService> logger) : base(client, logger)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for the client to be ready before setting the status
            await Client.WaitForReadyAsync(stoppingToken);
            Logger.LogInformation("Client is ready!");

            await Client.SetActivityAsync(new Game("Рублю пацанам мешапы🤠"));
        }
    }
}
