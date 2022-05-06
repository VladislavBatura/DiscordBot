using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Configuration;

namespace DiscordBot
{
    public class Youtube
    {
        private readonly YouTubeService _youtube;
        public string Id = "";
        public string Url => $"https://www.youtube.com/watch?v={Id}";
        public Youtube(IConfiguration config)
        {
            _youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = config["youtubeToken"],
                ApplicationName = GetType().ToString()
            });
        }

        public async Task<IEnumerable<Video>> SearchVideo(string query, int maxResults = 10)
        {
            if (query is null || string.IsNullOrEmpty(query))
            {
                return Enumerable.Empty<Video>();
            }

            var src = _youtube.Search.List("snippet");
            src.Q = query;
            src.MaxResults = maxResults + 50;

            var response = await src.ExecuteAsync();

            var videos = new List<Video>();

            foreach (var item in response.Items)
            {
                switch (item.Id.Kind)
                {
                    case "youtube#video":
                        videos.Add(new()
                        {
                            Id = item.Id.VideoId,
                            Title = item.Snippet.Title
                        });
                        break;
                    default:
                        break;
                }
            }

            return videos;
        }
    }
}
