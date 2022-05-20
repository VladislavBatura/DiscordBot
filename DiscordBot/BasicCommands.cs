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
using Microsoft.Extensions.Logging;
using DiscordBot.HostedServices;
using DiscordBot.Models;
using System.Diagnostics;

namespace DiscordBot
{
    public class BasicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly YoutubeClient _client;
        private readonly Storage _storage;
        private readonly MusicService _musicService;
        private readonly AudioGuildManager _audioGuildManager;
        private readonly VkApi _vk;
        private readonly ILogger<BasicCommands> _logger;
        private readonly VkApiService _vkService;

        public BasicCommands(YoutubeClient client,
                             Storage storage,
                             MusicService musicService,
                             AudioGuildManager audioManager,
                             VkApi vk,
                             ILogger<BasicCommands> logger,
                             VkApiService vkService)
        {
            _client = client;
            _storage = storage;
            _musicService = musicService;
            _audioGuildManager = audioManager;
            _vk = vk;
            _logger = logger;
            _vkService = vkService;
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

        [SlashCommand("loginvk", "Логинится в вк через двухфакторку", runMode: RunMode.Async)]
        public async Task LoginVk(string twoFactorCode)
        {
            if (string.IsNullOrEmpty(twoFactorCode) || twoFactorCode.Trim().Length != 6)
            {
                await RespondAsync("Введи код");
                return;
            }

            if (_storage.IsVkEnabled)
            {
                await RespondAsync("Уже включено");
                return;
            }

            var resultOfLogin = await _vkService.EnableVk(twoFactorCode);

            if (!resultOfLogin)
            {
                await RespondAsync("Не получилось");
                return;
            }
            await RespondAsync("Всё окей, пробуй");
            return;
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

            for (var i = 0; i < audios.Count; i++)
            {
                stringa = stringa.AppendLine($"{i + 1} -" +
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

            var audioClient = _storage.GetChannel(Context.Guild.Id);

            var audioManager = _audioGuildManager.GetGuildVoiceState(Context.Guild);

            audioManager.VkPlayer.SetAudioClient(audioClient);

            var vkTracks = audios.Select(x => new AudioTrackVk() { Audio = x }).ToList();

            foreach (var track in vkTracks)
            {
                await audioManager.VkScheduler.Enqueue(track);
            }

            //using var discord = audioClient.CreatePCMStream(AudioApplication.Mixed);

            //foreach (var audio in audios)
            //{
            //    if (File.Exists("output.mp3"))
            //        File.Delete("output.mp3");

            //    //Премного благодарен рандомным ребятам с гитхаба за этот фикс "проглоченных" фрагментов стрима
            //    _ = await Cli.Wrap("ffmpeg")
            //        .WithArguments($"-hide_banner -loglevel panic -http_persistent false -i \"{audio.Url}\" -c copy output.mp3")
            //        .ExecuteAsync();

            //    _ = await Cli.Wrap("ffmpeg")
            //        .WithArguments($"-hide_banner -loglevel panic -i output.mp3 -ac 2 -f s16le -ar 48000 pipe:1")
            //        .WithStandardOutputPipe(PipeTarget.ToStream(_storage.OutputStream))
            //        .ExecuteAsync();


            //    //багает стрим похоже, ибо он повторяется и смешивается. Разберись потом
            //    try
            //    {
            //        await discord.WriteAsync((_storage.OutputStream as MemoryStream)
            //            .ToArray()
            //            .AsMemory(0, (int)(_storage.OutputStream as MemoryStream).Length));
            //    }
            //    finally
            //    {
            //        await discord.FlushAsync();
            //        await _storage.OutputStream.FlushAsync();
            //        _storage.OutputStream.Position = 0;
            //        discord.Position = 0;
            //        File.Delete("output.mp3");
            //    }
            //}
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
