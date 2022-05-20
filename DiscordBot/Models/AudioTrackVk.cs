using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models
{
    public class AudioTrackVk : IDisposable
    {
        public byte[] BufferFrame = new byte[1024];
        public VkNet.Model.Attachments.Audio Audio { get; set; }
        public Stream SourceStream { get; set; }
        public Process Process { get; set; }

        public void LoadProcess()
        {
            var filename = $"/bin/bash";
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
                Task.Delay(100);
            }

            Process?.Dispose();

            command = $"/C ffmpeg -hide_banner -loglevel panic" +
                $" -i output.mp3 -ac 2 -f s16le -ar 48000 pipe:1";

            Process = Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            SourceStream = Process.StandardOutput.BaseStream;
        }

        public async Task<int> ReadAudioStream(CancellationToken ct)
        {
            return await SourceStream.ReadAsync(BufferFrame, 0, BufferFrame.Length, ct).ConfigureAwait(false);
        }

        public byte[] GetBufferFrame()
        {
            return BufferFrame;
        }

        public void Dispose()
        {
            SourceStream.Dispose();
            Process.Dispose();
            SourceStream = null;
            Process = null;
        }
    }
}
