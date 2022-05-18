using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Video
    {
        public string Id { get; set; }
        public string Url => $"https://www.youtube.com/watch?v={Id}";

        public string Title { get; set; }
    }
}
