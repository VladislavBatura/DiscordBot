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
using VkNet;
using VkNet.Utils;

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
        private readonly VkApi _vk;

        public BasicCommands(CommandHandler handler,
                             YoutubeClient client,
                             Storage storage,
                             Youtube youtube,
                             MusicService musicService,
                             AudioGuildManager audioManager,
                             VkApi vk)
        {
            _handler = handler;
            _client = client;
            _storage = storage;
            _youtube = youtube;
            _musicService = musicService;
            _audioGuildManager = audioManager;
            _vk = vk;
        }

        [SlashCommand("say", "Повторяет сообщение")]
        public async Task SayAsync(string echo)
        {
            await RespondAsync(echo);
        }

        [SlashCommand("play", "Ищет аудио в ютубе и играет его", false, RunMode.Async)]
        public async Task SearchAudioAsync(string name,
                                           int count = 10,
                                           IVoiceChannel? channel = null)
        {
            await DeferAsync();

            if (string.IsNullOrEmpty(name))
            {
                await ModifyMessage("Введи что-нибудь в поиск, дурбелик", null);
                return;
            }
            count = count > 15 ? 15 : count;

            var components = await SearchVideoOptionAsync(name, count);

            if (components is null)
            {
                await ModifyMessage("Ничего не нашло, давай по новой", null);
                return;
            }

            channel ??= (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                _ = await Context.Channel
                    .SendMessageAsync("Пользователь должен быть в голосовом канале" +
                    " или голосовой канал должен быть передан как параметр");
                return;
            }

            var embed = components.embedBuilder.Build();
            var selectOption = components.componentBuilder.Build();
            await ModifyMessage(null, embed);

            var m = await ReplyAsync("Выбери трек", components: selectOption);
            _storage.MessageId = m.Id;

            await _musicService.JoinAudio(Context.Guild, channel);
        }

        [SlashCommand("stop", "Стопит аудио", runMode: RunMode.Async)]
        public async Task StopAudioAsync()
        {
            await RespondAsync("Stopping the music");
            var audioManager = _audioGuildManager.GetGuildVoiceState(Context.Guild);
            await audioManager.Scheduler.Stop();
        }

        [SlashCommand("skip", "Скипает аудио", runMode: RunMode.Async)]
        public async Task SkipAudioAsync()
        {
            await RespondAsync("Skipping current track");
            var audioManager = _audioGuildManager.GetGuildVoiceState(Context.Guild);
            await audioManager.Scheduler.NextTrack();
        }

        [SlashCommand("playvk", "Играет музыку из вк", runMode: RunMode.Async)]
        public async Task PlayVkAudioAsync(int count = 10,
                                           long userId = default,
                                           long offset = 0,
                                           IVoiceChannel? channel = null)
        {
            await DeferAsync();

            if (!_storage.IsVkEnabled)
            {
                _ = await ModifyOriginalResponseAsync((x) =>
                  {
                      x.Content = "Вк не включён. Попроси Aviator'а включить.";
                  });
                return;
            }

            _ = PlayVkMusicAsync(count, userId, offset, channel);
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
                .WithPlaceholder("Выбери опцию")
                .WithCustomId("musicMenu")
                .WithMinValues(1)
                .WithMaxValues(1);


            var embed = new EmbedBuilder
            {
                Title = $"Поиск по {searchQuery}",
                Description = $"В общем количестве {filteredVideos.Count}"
            };
            var stringa = new StringBuilder();
            var i = 1;
            foreach (var video in filteredVideos)
            {

                stringa = stringa.AppendLine($"{i} - {video.Title} - ({video.Duration})");

                var title = video.Title.Length > 80 ? video.Title[0..80] : video.Title;

                menuBuilder = menuBuilder.AddOption($"{i} - {title}",
                    video.Url,
                    $"({video.Duration})");
                i++;
            }

            embed = embed.AddField("Результаты", stringa)
                .WithAuthor(Context.Client.CurrentUser)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold);

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            var components = new MessageComponent(embed, builder);

            _storage.AddData(Context.User.Id, videosUrl);
            return components;
        }

        private async Task PlayVkMusicAsync(int count = 10,
                                           long userId = default,
                                           long offset = 0,
                                           IVoiceChannel? channel = null)
        {
            count = count > 6000 ? 6000 : count;
            offset = offset > 6000 ? 6000 : offset;
            var audios = userId is 0
                ? _vk.Audio.Get(new()
                {
                    Count = count,
                    Offset = offset
                })
                : _vk.Audio.Get(new()
                {
                    Count = count,
                    OwnerId = userId,
                    Offset = offset
                });

            if (!audios.Any())
            {
                _ = await ModifyOriginalResponseAsync((x) =>
                {
                    x.Content = "Не получилось забрать музыку." +
                    " Либо серваки легли, либо ты заюзал неправильный id." +
                    " Используй циферки из адресной строки на странице своих аудио, либо ничего не трогай";
                });
                return;
            }

            var embedBuilder = new EmbedBuilder()
            {
                Title = $"{count} аудиозаписей из вк со смещенией {offset}"
            };

            var stringa = new StringBuilder();

            for (var i = 1; i <= 10; i++)
            {
                stringa = stringa.AppendLine($"{i} -" +
                    $" {audios[i].Title} -" +
                    $" {audios[i].Artist} - " +
                    $"({audios[i].Duration})");
            }

            embedBuilder = embedBuilder
                .AddField("Результат", stringa)
                .WithAuthor(Context.Client.CurrentUser)
                .WithCurrentTimestamp()
                .WithColor(Color.Gold);

            var embed = embedBuilder.Build();

            _ = await ModifyOriginalResponseAsync((x) =>
            {
                x.Embed = embed;
                x.Content = string.Empty;
            });

            channel ??= (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null)
            {
                _ = await Context.Channel
                    .SendMessageAsync("User must be in a voice channel," +
                    " or a voice channel must be passed as an argument.");
                return;
            }

            _storage.OutputStream = new MemoryStream();

            await _musicService.JoinAudio(Context.Guild, channel);

            foreach (var audio in audios)
            {
                _ = await Cli.Wrap("ffmpeg")
                    .WithArguments($"-hide_banner -loglevel panic -i {audio.Url} -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardOutputPipe(PipeTarget.ToStream(_storage.OutputStream))
                    .ExecuteAsync();

                var audioClient = _storage.GetChannel(Context.Guild.Id);

                using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);
                try
                {
                    await discord.WriteAsync((_storage.OutputStream as MemoryStream)
                        .ToArray()
                        .AsMemory(0, (int)(_storage.OutputStream as MemoryStream).Length));
                }
                finally
                {
                    await discord.FlushAsync();
                    await _storage.OutputStream.FlushAsync();
                }
            }
            return;
        }

        private async Task ModifyMessage(string? message, Embed? embed)
        {
            _ = await ModifyOriginalResponseAsync((x) =>
            {
                x.Content = message;
                x.Embed = embed;
            });
        }
    }
}
