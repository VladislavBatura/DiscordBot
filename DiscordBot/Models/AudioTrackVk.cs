using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Models
{
    public class AudioTrackVk : IDisposable
    {
        private readonly byte[] _bufferFrame = new byte[1024];
        private readonly ILogger<AudioTrackVk> _logger;

        public VkNet.Model.Attachments.Audio? Audio { get; set; }
        public Stream? SourceStream { get; set; }
        public Process? Process { get; set; }

        public AudioTrackVk()
        {
            var factory = LoggerFactory.Create(a => a.AddConsole());
            _logger = factory.CreateLogger<AudioTrackVk>();
        }

        public async void LoadProcess()
        {
            if (Audio is null)
            {
                LogWarning(nameof(Audio));
                return;
            }

            var filename = $"/bin/bash";
            //https://gist.github.com/grwlf/e1876f5d78cb6e66791809771d7bf36b?permalink_comment_id=3147977#gistcomment-3147977
            //Это ответ помог грузить m3u8 с вк без "проглатываний" участков аудио
            var command = $"-c \"ffmpeg -hide_banner -loglevel panic -http_persistent false" +
                $" -i \"{Audio.Url}\" -c copy output.mp3\"";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filename = "cmd.exe";
                command = $"/C ffmpeg -hide_banner -loglevel panic -http_persistent false" +
                    $" -i \"{Audio.Url}\" -c copy output.mp3";
            }

            if (File.Exists("output.mp3"))
                File.Delete("output.mp3");

            Process = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            while (!File.Exists("output.mp3"))
            {
                _ = Task.Delay(100);
            }

            Process?.Dispose();

            command = $"/C ffmpeg -hide_banner -loglevel panic" +
                $" -i output.mp3 -ac 2 -f s16le -ar 48000 pipe:1";

            Process = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (Process is null)
            {
                LogWarning(nameof(Process));
                return;
            }

            SourceStream = Process.StandardOutput.BaseStream;
        }

        public async Task<int> ReadAudioStream(CancellationToken ct)
        {
            if (SourceStream is null)
            {
                LogWarning(nameof(SourceStream));
                return -1;
            }

            return await SourceStream.ReadAsync(_bufferFrame, ct).ConfigureAwait(false);
        }

        public byte[] GetBufferFrame()
        {
            return _bufferFrame;
        }

        public void Dispose()
        {
            SourceStream?.Dispose();
            Process?.Dispose();
            SourceStream = null;
            Process = null;
            GC.SuppressFinalize(this);
        }

        private void LogWarning(string nameOfVar)
        {
            _logger.LogWarning($"Call from {this}, {nameOfVar} is null");
        }
    }
}
