using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Discord.Audio;

namespace DiscordBot
{
    public class Storage
    {
        private readonly ConcurrentDictionary<ulong, IEnumerable<string>> _youtubeStorage
            = new();
        private readonly ConcurrentDictionary<ulong, IAudioClient> _channels = new();
        public string Url { get; set; } = string.Empty;
        public bool IsVkEnabled { get; set; }
        public ulong MessageId { get; set; }
        public Stream InputStream { get; set; }
        public Stream OutputStream { get; set; }

        public void AddData(ulong id, IEnumerable<string> searchResults)
        {
            _ = _youtubeStorage.AddOrUpdate(id, searchResults, (Id, result) =>
            {
                result = searchResults;
                return result;
            });
            DiscardResultAfterTime(id);
        }

        public void AddChannel(ulong id, IAudioClient client)
        {
            _ = _channels.AddOrUpdate(id, client, (Id, result) =>
            {
                result = client;
                return result;
            });
        }

        public void RemoveData(ulong id)
        {
            _ = _youtubeStorage.TryRemove(id, out _);
        }

        public IAudioClient? RemoveChannel(ulong id)
        {
            _ = _channels.TryRemove(id, out var channel);
            return channel;
        }

        public string GetData(ulong id, int position)
        {
            _ = _youtubeStorage.TryGetValue(id, out var result);
            return result.ElementAtOrDefault(position - 1) ?? string.Empty;
        }

        public IAudioClient GetChannel(ulong id)
        {
            _ = _channels.TryGetValue(id, out var result);
            return result;
        }

        public bool IsExist(ulong id)
        {
            return _youtubeStorage.ContainsKey(id);
        }

        public bool ChannelExist(ulong id)
        {
            return _channels.ContainsKey(id);
        }

        private void DiscardResultAfterTime(ulong id)
        {
            Task.Delay(600_000)
                .ContinueWith(t => RemoveData(id));
        }
    }
}
