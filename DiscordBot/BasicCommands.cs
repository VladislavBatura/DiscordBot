using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord;
using Discord.Audio;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using CliWrap;
using VkNet.Model.Attachments;
using VkNet;

namespace DiscordBot
{
    public class BasicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly CommandHandler _handler;
        private readonly YoutubeClient _client;
        private readonly Storage _storage;

        public BasicCommands(CommandHandler handler, YoutubeClient client, Storage storage)
        {
            _handler = handler;
            _client = client;
            _storage = storage;
        }

        [SlashCommand("say", "Echoes message")]
        public async Task SayAsync(string echo)
        {
            await RespondAsync(echo);
        }

        [SlashCommand("play", "Searches audio in youtube and plays it", false, RunMode.Async)]
        public async Task SearchAudioAsync(string name, int count = 10, IVoiceChannel channel = null)
        {
            await DeferAsync();
            var embed = await SearchVideoAsync(name, count);
            var embeD = embed.Build();
            var msg = await ModifyOriginalResponseAsync((ms) =>
            {
                ms.Embed = embeD;
            });

            channel ??= (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                _ = await Context.Channel
                    .SendMessageAsync("User must be in a voice channel," +
                    " or a voice channel must be passed as an argument.");
                return;
            }

            var audioClient = await channel.ConnectAsync();
            _storage.AddChannel(Context.User.Id, audioClient);
            while (_storage.url.Equals(""))
            {
                Console.WriteLine("waiting user input...");
                await Task.Delay(500);
            }

            var s = await _client.Videos.Streams.GetManifestAsync(_storage.url);
            var info = s.GetAudioOnlyStreams().GetWithHighestBitrate();
            var si = await _client.Videos.Streams.GetAsync(info);

            var memoryStream = new MemoryStream();
            var o = await Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(si))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();

            await Task.Delay(2_000);

            using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try
                {
                    await discord.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                }
                finally
                {
                    await discord.FlushAsync();
                }
            }
        }

        async Task<EmbedBuilder> SearchVideoAsync(string name, int count)
        {
            var client = new YoutubeClient();
            var videos = await client.Search.GetVideosAsync(name);
            var slicedVideos = videos.Take(count).ToList();
            var videosUrl = new List<string>();

            foreach (var video in slicedVideos)
            {
                videosUrl.Add(video.Url);
            }

            var embed = new EmbedBuilder
            {
                Title = $"Search by {name}",
                Description = $"Total in {slicedVideos.Count}"
            };
            var stringa = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                stringa.AppendLine($"{i + 1} - {slicedVideos[i].Title} - ({slicedVideos[i].Duration})");
            }

            embed.AddField("Results", stringa)
                .WithAuthor(Context.Client.CurrentUser)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold);

            _storage.AddData(Context.User.Id, videosUrl);
            return embed;

            
        }
    }
}
