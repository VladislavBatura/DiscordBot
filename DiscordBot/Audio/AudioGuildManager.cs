using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Music.Common;
using Discord.Audio;
using DiscordBot.Audio;
using DiscordBot.Models;
using VkNet.Utils;

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

        public async Task PlayMusic(IGuild guild, IAudioClient audioClient, string url)
        {
            var audioManager = GetGuildVoiceState(guild);

            audioManager.Player.SetAudioClient(audioClient);

            var tracks = await TrackLoader.LoadAudioTrack(url, true);

            var castedTracks = tracks
                .Select(x =>
                {
                    var y = new AudioTrackSecond(false, x.Url);
                    y.Info = x.Info;
                    return y;
                })
                .ToList();

            foreach (var track in castedTracks)
            {
                await audioManager.Scheduler.Enqueue(track);
            }
        }

        public async Task PlayMusicVk(IGuild guild,
                                      IAudioClient audioClient,
                                      VkCollection<VkNet.Model.Attachments.Audio> audios)
        {
            var audioManager = GetGuildVoiceState(guild);

            audioManager.Player.SetAudioClient(audioClient);

            var tracks = audios.Select(x =>
            {
                var y = new AudioTrackSecond(true, x.Url.ToString());
                y.Info = new AudioInfo()
                {
                    Url = y.Url,
                    Author = x.Artist,
                    Duration = x.Duration.ToString(),
                    Title = x.Title
                };
                return y;
            })
            .ToList();

            foreach (var track in tracks)
            {
                await audioManager.Scheduler.Enqueue(track);
            }
        }
    }
}
