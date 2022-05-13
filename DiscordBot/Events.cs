using Discord;
using Discord.Addons.Music.Common;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class Events
    {
        private readonly Storage _storage;
        private readonly MusicService _musicService;
        private readonly AudioGuildManager _audioGuildManager;

        public Events(Storage storage, MusicService musicService, AudioGuildManager audioGuildManager)
        {
            _storage = storage;
            _musicService = musicService;
            _audioGuildManager = audioGuildManager;
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

            _ = _audioGuildManager.PlayMusic(chnl.Guild,
                _storage.GetChannel(chnl.Guild.Id),
                _storage.Url).ConfigureAwait(false);
            return Task.CompletedTask;
        }
    }
}
