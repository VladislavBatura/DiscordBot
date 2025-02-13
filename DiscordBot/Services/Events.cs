﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DiscordBot.Services
{
    public class Events
    {
        private readonly Storage _storage;
        private readonly AudioGuildManager _audioGuildManager;
        private readonly InteractionService _commands;

        public Events(Storage storage,
                      AudioGuildManager audioGuildManager,
                      InteractionService commands)
        {
            _storage = storage;
            _audioGuildManager = audioGuildManager;
            _commands = commands;
        }
        public Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }

        public Task CatchMessage(SocketMessage msg)
        {
            if (msg is SocketUserMessage)
            {
                if (_storage.IsExist(msg.Author.Id))
                {
                    if (!int.TryParse(msg.Content.Trim(), out var number))
                    {
                        _ = msg.Channel.SendMessageAsync("Choose from given numbers, durbelik");
                        return Task.CompletedTask;
                    }
                    var video = _storage.GetData(msg.Author.Id, number);
                    _storage.Url = video;
                    _storage.RemoveData(msg.Author.Id);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<Task> CatchSelectOption(SocketMessageComponent msg)
        {
            var text = string.Join(", ", msg.Data.Values);
            await msg.RespondAsync($"You have selected {text}");
            await msg.Channel.DeleteMessageAsync(_storage.MessageId);
            _storage.Url = text;
            var chnl = msg.Channel as SocketGuildChannel;

            _ = _audioGuildManager.PlayMusic(chnl!.Guild,
                _storage.GetChannel(chnl.Guild.Id),
                _storage.Url).ConfigureAwait(false);
            return Task.CompletedTask;
        }

        public async Task ReadyAsync()
        {
            //_ = await _commands.RegisterCommandsGloballyAsync();
            _ = await _commands.RegisterCommandsToGuildAsync(469853933427359747);
        }
    }
}
