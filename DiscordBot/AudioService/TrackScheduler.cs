using Discord.Addons.Music.Player;
using Discord.Addons.Music.Source;
using Discord.Audio;

namespace DiscordBot.AudioService
{
    public class TrackScheduler
    {
        private readonly AudioPlayer _player;

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
                _ = _player.StartTrackAsync(track).ConfigureAwait(false);
            }
            return Task.CompletedTask;
        }

        public async Task NextTrack()
        {
            if (SongQueue.TryDequeue(out var nextTrack))
                await _player.StartTrackAsync(nextTrack);
            else
                _player.Stop();

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
