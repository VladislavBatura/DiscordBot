using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Object;

namespace DiscordBot.Models
{
    public class AudioInfo : IAudioInfo
    {
        public string Url { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string Duration { get; set; } = "";
    }
}
