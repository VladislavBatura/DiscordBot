using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Player;
using DiscordBot.AudioService;
using DiscordBot.Models;

namespace DiscordBot.Audio
{
    public class AudioManager
    {
        public AudioPlayer Player { get; set; }
        public TrackScheduler Scheduler { get; set; }
        public AudioTrackSecond VkPlayer { get; set; }
        public TrackSchedulerVk VkScheduler { get; set; }


        public AudioManager()
        {
            Player = new AudioPlayer();
            Scheduler = new TrackScheduler(Player);
            VkPlayer = new();
            VkScheduler = new(VkPlayer);
        }
    }
}
