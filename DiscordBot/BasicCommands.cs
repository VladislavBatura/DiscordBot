using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Common;

namespace DiscordBot
{
    public class BasicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private readonly CommandHandler _handler;
        private readonly YoutubeClient _client;

        private IEnumerable<VideoSearchResult> videoSearchResults;

        public BasicCommands(CommandHandler handler, YoutubeClient client)
        {
            _handler = handler;
            _client = client;
        }

        [SlashCommand("say", "Echoes message")]
        public async Task SayAsync(string echo)
            => await RespondAsync(echo);

        [SlashCommand("play", "Searches audio in youtube and plays it", false, RunMode.Async)]
        public async Task SearchAudioAsync(string name, int count = 10)
        {
            await DeferAsync();
            var embed = await SearchVideoAsync(name, count);
            var embeD = embed.Build();
            var msg = await ModifyOriginalResponseAsync((ms) =>
            {
                ms.Embed = embeD;
            });
        }

        async Task<EmbedBuilder> SearchVideoAsync(string name, int count)
        {
            var client = new YoutubeClient();
            var videos = await client.Search.GetVideosAsync(name);
            var slicedVideos = videos.Take(count).ToList();
            videoSearchResults = slicedVideos;

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

            return embed;
        }
    }
}
