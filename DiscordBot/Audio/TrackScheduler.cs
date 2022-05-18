using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Player;
using Discord.Addons.Music.Source;
using Discord.Audio;
using DiscordBot.Models;

namespace DiscordBot.Audio
{
    public class TrackScheduler
    {
        private AudioPlayer _player;

        public Queue<AudioTrackSecond> SongQueue { get; set; }

        public TrackScheduler(AudioPlayer player)
        {
            _player = player;
            SongQueue = new Queue<AudioTrackSecond>();
            _player.OnTrackStartAsync += OnTrackStartAsync;
            _player.OnTrackEndAsync += OnTrackEndAsync;
        }

        public async Task Enqueue(AudioTrackSecond track)
        {
            if (_player.PlayingTrack != null)
            {
                SongQueue.Enqueue(track);
            }
            else
            {
                // fire and forget
                await _player.StartTrackAsync(track).ConfigureAwait(false);
            }
            return;
        }

        public async Task NextTrack()
        {
            if (_player.PlayingTrack is not null)
            {
                await Stop();
                if (SongQueue.TryDequeue(out var nextTrack))
                    await _player.StartTrackAsync(nextTrack);
            }
            else
            {
                if (SongQueue.TryDequeue(out var nextTrack))
                    await _player.StartTrackAsync(nextTrack);
                else
                    _player.Stop();
            }
        }

        public Task Stop()
        {
            _player.Stop();
            return Task.CompletedTask;
        }

        private Task OnTrackStartAsync(IAudioClient audioClient, IAudioSource track)
        {
            Console.WriteLine("Track start! " + track.Info.Title);
            return Task.CompletedTask;
        }

        private async Task OnTrackEndAsync(IAudioClient audioClient, IAudioSource track)
        {
            Console.WriteLine("Track end! " + track.Info.Title);

            await NextTrack();
        }
    }
}
