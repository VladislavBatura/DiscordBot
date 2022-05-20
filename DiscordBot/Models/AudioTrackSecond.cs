using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Source;
using Discord.Audio;

namespace DiscordBot.Models
{
    public class AudioTrackSecond
    {
        public event EventHandler<AudioTrackVkEventArgs> TrackStartEvent;
        public event EventHandler<AudioTrackVkEventArgs> TrackStopEvent;

        public Stream DiscordStream { get; set; }
        public IAudioClient AudioClient { get; private set; }
        public AudioTrackVkEventArgs AudioTrack { get; set; }
        private CancellationTokenSource _cancelToken;
        public Task TrackStartEventAsync(AudioTrackVkEventArgs e)
        {
            e.Audio.LoadProcess();
            TrackStartEvent(this, e);
            return Task.CompletedTask;
        }

        private async Task ReadAudio(AudioTrackVkEventArgs audioTrack, CancellationToken ct)
        {
            var read = -1;
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
                read = await AudioTrack.Audio.ReadAudioStream(ct).ConfigureAwait(false);
                if (read > 0)
                {
                    await DiscordStream.WriteAsync(AudioTrack.Audio.GetBufferFrame(), 0, read, ct).ConfigureAwait(false);
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

        public async Task Stop()
        {
            try
            {
                _cancelToken?.Cancel(false);
            }
            catch (ObjectDisposedException)
            {
            }
            _cancelToken?.Dispose();
        }

        void ResetStreams()
        {
            DiscordStream?.Flush();
            AudioTrack?.Dispose();
        }

        ~AudioTrackSecond()
        {
            DiscordStream?.Dispose();
            AudioTrack?.Dispose();
            _cancelToken?.Dispose();
        }
    }

    public class AudioTrackVkEventArgs : EventArgs, IDisposable
    {
        public AudioTrackVk Audio { get; set; }

        public void Dispose()
        {
            Audio.Dispose();
        }
    }
}
