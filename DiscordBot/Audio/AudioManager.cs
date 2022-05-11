using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Addons.Music.Player;

namespace DiscordBot.Audio
{
    public class AudioManager
    {
        public AudioPlayer Player { get; set; }
        public TrackScheduler Scheduler { get; set; }

        public AudioManager()
        {
            Player = new AudioPlayer();
            Scheduler = new TrackScheduler(Player);
        }
    }
}
