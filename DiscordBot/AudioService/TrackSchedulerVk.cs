using DiscordBot.Models;

namespace DiscordBot.AudioService
{
    public class TrackSchedulerVk
    {
        private readonly VkPlayer _player;

        public Queue<AudioTrackVk> SongQueue { get; set; }

        public TrackSchedulerVk(VkPlayer player)
        {
            _player = player;
            SongQueue = new Queue<AudioTrackVk>();
            _player.TrackStartEvent += OnTrackStartAsync;
            _player.TrackStopEvent += OnTrackEndAsync;
        }

        public Task Enqueue(AudioTrackVk track)
        {
            if (_player.AudioTrack != null)
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
                _ = _player.Stop();
        }

        public Task Stop()
        {
            _ = _player.Stop();
            return Task.CompletedTask;
        }

        public void OnTrackStartAsync(object sender, AudioTrackVkEventArgs e)
        {
            Console.WriteLine($"Track start! {e.Audio!.Audio!.Title} - {e.Audio.Audio.Artist}");
        }

        public async void OnTrackEndAsync(object sender, AudioTrackVkEventArgs e)
        {
            Console.WriteLine($"Track end! {e.Audio!.Audio!.Title} - {e.Audio.Audio.Artist}");

            await NextTrack();
        }
    }
}
