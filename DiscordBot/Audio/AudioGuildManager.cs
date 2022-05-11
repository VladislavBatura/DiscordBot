using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Audio;

namespace DiscordBot
{
    public class AudioGuildManager
    {
        public readonly ConcurrentDictionary<ulong, AudioManager> VoiceStates = new();

        public AudioManager GetGuildVoiceState(IGuild guild)
        {
            AudioManager voiceState;

            if (!VoiceStates.ContainsKey(guild.Id))
            {
                VoiceStates.TryAdd(guild.Id, new AudioManager());
            }
            voiceState = VoiceStates[guild.Id];

            return voiceState;
        }
    }
}
