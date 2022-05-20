using Discord.Audio;

namespace DiscordBot.Models
{
    public class VkPlayer : IDisposable
    {
        public event EventHandler<AudioTrackVkEventArgs> TrackStartEvent;
        public event EventHandler<AudioTrackVkEventArgs> TrackStopEvent;

        public Stream? DiscordStream { get; set; }
        public IAudioClient? AudioClient { get; private set; }
        public AudioTrackVkEventArgs? AudioTrack { get; set; }
        private CancellationTokenSource? _cancelToken;
        public Task TrackStartEventAsync(AudioTrackVkEventArgs e)
        {
            if (e is null || e.Audio is null)
            {
                return Task.CompletedTask;
            }

            e.Audio.LoadProcess();
            TrackStartEvent(this, e);
            return Task.CompletedTask;
        }

        private async Task ReadAudio(AudioTrackVkEventArgs audioTrack, CancellationToken ct)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                if (DiscordStream == null)
                {
                    Console.WriteLine("Discord stream is gone");
                    return;
                }
                // Read audio byte sample
                var read = await audioTrack!.Audio!.ReadAudioStream(ct).ConfigureAwait(false);
                if (read > 0)
                {
                    await DiscordStream.WriteAsync(audioTrack.Audio.GetBufferFrame().AsMemory(0, read), ct).ConfigureAwait(false);
                }
                // Finished playing
                else
                {
                    return;
                }
            }
        }

        public Task TrackEndEventAsync(AudioTrackVkEventArgs e)
        {
            ResetStreams();
            AudioTrack = null;
            _cancelToken?.Dispose();
            TrackStopEvent(this, e);
            return Task.CompletedTask;
        }

        public void SetAudioClient(IAudioClient client)
        {
            AudioClient = client;
            DiscordStream?.Dispose();
            DiscordStream = client.CreatePCMStream(AudioApplication.Mixed);
        }

        public async Task StartTrackAsync(AudioTrackVk track)
        {
            if (track is null)
                return;

            if (AudioTrack is not null)
            {
                _ = Stop();
            }

            AudioTrack = new AudioTrackVkEventArgs()
            {
                Audio = track
            };

            _cancelToken?.Dispose();
            _cancelToken = new CancellationTokenSource();

            await TrackStartEventAsync(AudioTrack).ConfigureAwait(false);
            await ReadAudio(AudioTrack, _cancelToken.Token).ConfigureAwait(false);
            await TrackEndEventAsync(AudioTrack).ConfigureAwait(false);
        }

        public Task Stop()
        {
            try
            {
                _cancelToken?.Cancel(false);
            }
            catch (ObjectDisposedException)
            {
            }
            _cancelToken?.Dispose();
            AudioTrack?.Dispose();
            return Task.CompletedTask;
        }

        private void ResetStreams()
        {
            DiscordStream?.Flush();
            AudioTrack?.Dispose();
        }

        ~VkPlayer()
        {
            DiscordStream?.Dispose();
            AudioTrack?.Dispose();
            _cancelToken?.Dispose();
        }

        public void Dispose()
        {
            DiscordStream?.Dispose();
            AudioTrack?.Dispose();
            _cancelToken?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
