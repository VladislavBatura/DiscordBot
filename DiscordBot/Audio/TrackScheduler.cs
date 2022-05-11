using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Player;
using Discord.Addons.Music.Source;
using Discord.Audio;

namespace DiscordBot.Audio
{
    public class TrackScheduler
    {
        private AudioPlayer _player;

        public Queue<AudioTrack> SongQueue { get; set; }

        public TrackScheduler(AudioPlayer player)
        {
            _player = player;
            SongQueue = new Queue<AudioTrack>();
            _player.OnTrackStartAsync += OnTrackStartAsync;
            _player.OnTrackEndAsync += OnTrackEndAsync;
        }

        public Task Enqueue(AudioTrack track)
        {
            if (_player.PlayingTrack != null)
            {
                SongQueue.Enqueue(track);
            }
            else
            {
                // fire and forget
                _player.StartTrackAsync(track).ConfigureAwait(false);
            }
            return Task.CompletedTask;
        }

        public async Task NextTrack()
        {
            AudioTrack nextTrack;
            if (SongQueue.TryDequeue(out nextTrack))
                await _player.StartTrackAsync(nextTrack);
            else
                _player.Stop();
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
