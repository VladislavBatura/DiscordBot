using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Source;

namespace DiscordBot.Models
{
    public class AudioTrackSecond : AudioTrack
    {
        public bool IsFromVk { get; set; }

        public AudioTrackSecond(bool isFromVk, string url)
        {
            IsFromVk = isFromVk;
            if (string.IsNullOrEmpty(Url))
            {
                Url = url;
            }
        }

        public override void LoadProcess()
        {
            var fileName = "/bin/bash";
            var arguments = "-c \"youtube-dl --format bestaudio -o - " + base.Url + " | ffmpeg -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1\"";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName = "cmd.exe";
                arguments = "/C youtube-dl.exe --format bestaudio --audio-quality 0 -o - " + base.Url + " | ffmpeg.exe -loglevel warning -re -vn -i pipe:0 -f s16le -b:a 128k -ar 48000 -ac 2 pipe:1";
            }
            if (IsFromVk)
            {
                arguments = $"ffmpeg.exe -hide_banner -loglevel panic -i {Url} -ac 2 -f s16le -ar 48000 pipe:1";
            }

            base.FFmpegProcess = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            base.SourceStream = base.FFmpegProcess.StandardOutput.BaseStream;
        }
    }
}
