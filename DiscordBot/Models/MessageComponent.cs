using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordBot
{
    public class MessageComponent
    {
        public readonly EmbedBuilder embedBuilder;
        public readonly ComponentBuilder componentBuilder;

        public MessageComponent(EmbedBuilder embedBuilder, ComponentBuilder componentBuilder)
        {
            this.embedBuilder = embedBuilder;
            this.componentBuilder = componentBuilder;
        }
    }
}
