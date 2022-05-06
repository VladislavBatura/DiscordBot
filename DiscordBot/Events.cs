using Discord;
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
        public Events(Storage storage)
        {
            _storage = storage;
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
                    _storage.url = video;
                    _storage.RemoveData(msg.Author.Id);
                }
            }
            return Task.CompletedTask;
        }
    }
}
