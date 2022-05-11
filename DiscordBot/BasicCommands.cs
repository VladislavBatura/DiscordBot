using System.Text;
using Discord.Interactions;
using Discord;
using Discord.Audio;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using CliWrap;
using YoutubeExplode.Search;
using Discord.Addons.Music.Player;
using Discord.Addons.Music.Source;
using Discord.Addons.Music.Common;
using Discord.Addons.Music.Objects;

namespace DiscordBot
{
    public class BasicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly CommandHandler _handler;
        private readonly YoutubeClient _client;
        private readonly Storage _storage;
        private readonly Youtube _youtube;
        private readonly MusicService _musicService;
        private readonly AudioGuildManager _audioGuildManager;

        public BasicCommands(CommandHandler handler,
                             YoutubeClient client,
                             Storage storage,
                             Youtube youtube,
                             MusicService musicService,
                             AudioGuildManager audioManager)
        {
            _handler = handler;
            _client = client;
            _storage = storage;
            _youtube = youtube;
            _musicService = musicService;
            _audioGuildManager = audioManager;
        }

        [SlashCommand("say", "Echoes message")]
        public async Task SayAsync(string echo)
        {
            await RespondAsync(echo);
        }

        [SlashCommand("play", "Searches audio in youtube and plays it", false, RunMode.Async)]
        public async Task SearchAudioAsync(string name, int count = 10, IVoiceChannel? channel = null)
        {
            await DeferAsync();
            //var embedBuilder = await SearchVideoAsync(name, count);
            //if (embedBuilder is null)
            //{
            //    _ = await ModifyOriginalResponseAsync((ms) =>
            //      {
            //          ms.Content = "Failed to search";
            //      });
            //    return;
            //}

            //var embed = embedBuilder.Build();
            //var msg = await ModifyOriginalResponseAsync((ms) =>
            //{
            //    ms.Embed = embed;
            //});

            var components = await SearchVideoOptionAsync(name, count);

            if (components is null)
            {
                _ = await ModifyOriginalResponseAsync((ms) =>
                  {
                      ms.Content = "Failed to search";
                  });
                return;
            }

            var embed = components.embedBuilder.Build();
            var selectOption = components.componentBuilder.Build();
            var msg = await ModifyOriginalResponseAsync((ms) =>
            {
                ms.Embed = embed;
            });

            var m = await ReplyAsync("Select track", components: selectOption);
            _storage.MessageId = m.Id;

            channel ??= (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                _ = await Context.Channel
                    .SendMessageAsync("User must be in a voice channel," +
                    " or a voice channel must be passed as an argument.");
                return;
            }

            await _musicService.JoinAudio(Context.Guild, channel);

            while (string.IsNullOrEmpty(_storage.Url))
            {
                Console.WriteLine("waiting user input...");
                await Task.Delay(1_000);
            }

            var audioManager = _audioGuildManager.GetGuildVoiceState(Context.Guild);

            audioManager.Player.SetAudioClient(_storage.GetChannel(Context.Guild.Id));

            var tracks = await TrackLoader.LoadAudioTrack(_storage.Url, true);

            await audioManager.Scheduler.Enqueue(tracks[0]);

            //await _musicService.PlayMusicAsync(Context);

            //await _musicService.LeaveAudio(Context.Guild);
        }

        private async Task<EmbedBuilder?> SearchVideoAsync(string searchQuery, int count)
        {
            var videos = new List<VideoSearchResult>();
            await foreach (var batch in _client.Search.GetResultBatchesAsync(searchQuery, SearchFilter.Video))
            {
                foreach (var video in batch.Items)
                {
                    if (!(video as VideoSearchResult).Duration.HasValue ||
                        (video as VideoSearchResult).Duration.Value.TotalMinutes
                        is > 125d or < 0.5d)
                    {
                        continue;
                    }
                    videos.Add((VideoSearchResult)video);
                }
            }

            var filteredVideos = videos
                .Take(count)
                .ToList();

            var videosUrl = new List<string>();

            foreach (var video in filteredVideos)
            {
                videosUrl.Add(video.Url);
            }

            var embed = new EmbedBuilder
            {
                Title = $"Search by {searchQuery}",
                Description = $"Total in {filteredVideos.Count}"
            };
            var stringa = new StringBuilder();
            var i = 1;
            foreach (var video in filteredVideos)
            {
                stringa = stringa.AppendLine($"{i} - {video.Title} - ({video.Duration})");
                i++;
            }

            embed = embed.AddField("Results", stringa)
                .WithAuthor(Context.Client.CurrentUser)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold);

            _storage.AddData(Context.User.Id, videosUrl);
            return embed;
        }

        private async Task<MessageComponent?> SearchVideoOptionAsync(string searchQuery, int count)
        {
            var videos = new List<VideoSearchResult>();
            await foreach (var batch in _client.Search.GetResultBatchesAsync(searchQuery, SearchFilter.Video))
            {
                foreach (var video in batch.Items)
                {
                    if (!(video as VideoSearchResult).Duration.HasValue ||
                        (video as VideoSearchResult).Duration.Value.TotalMinutes
                        is > 125d or < 0.5d)
                    {
                        continue;
                    }
                    videos.Add((VideoSearchResult)video);
                }
            }

            var filteredVideos = videos
                .Take(count)
                .ToList();

            var videosUrl = new List<string>();

            foreach (var video in filteredVideos)
            {
                videosUrl.Add(video.Url);
            }

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Select an option")
                .WithCustomId("musicMenu")
                .WithMinValues(1)
                .WithMaxValues(1);


            var embed = new EmbedBuilder
            {
                Title = $"Search by {searchQuery}",
                Description = $"Total in {filteredVideos.Count}"
            };
            var stringa = new StringBuilder();
            var i = 1;
            foreach (var video in filteredVideos)
            {
                stringa = stringa.AppendLine($"{i} - {video.Title} - ({video.Duration})");

                menuBuilder = menuBuilder.AddOption($"{i} - {video.Title}",
                    video.Url,
                    $"({video.Duration})");
                i++;
            }

            embed = embed.AddField("Results", stringa)
                .WithAuthor(Context.Client.CurrentUser)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold);

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            var components = new MessageComponent(embed, builder);

            _storage.AddData(Context.User.Id, videosUrl);
            return components;
        }
    }
}
