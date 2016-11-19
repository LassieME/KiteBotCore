using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using KiteBotCore.Json;

namespace KiteBotCore.Modules
{
    public class MarkovChain : CleansingModuleBase
    {
        [Command("testMarkov")]
        [Alias("tm")]
        [Summary("creates a Markov Chain string based on user messages")]
        [RequireServer(Server.KiteCo)]
        public async Task MarkovChainCommand(string haiku = null)
        {
            if (haiku == "haiku")
            {
                await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence() + "\n" +
                                 KiteChat.MultiDeepMarkovChains.GetSequence() + "\n" +
                                 KiteChat.MultiDeepMarkovChains.GetSequence() + "\n");
            }
            else
            {
                await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence());
            }
        }

        [Command("feed", RunMode = RunMode.Mixed)]
        [Summary("Downloads and feeds the markovchain")]
        [RequireOwner,RequireServer(Server.KiteCo)]
        public async Task FeedCommand(int amount)
        {
            var messages = Context.Channel.GetMessagesAsync(amount);
            int i = 0;
            ImmutableList<MarkovMessage> list = KiteChat.MultiDeepMarkovChains.GetFullDatabase();//
            await messages.ForEachAsync( collection =>
            {
                i++;
                foreach (var msg in collection)
                {
                    if (list.All(x => x.Id != msg.Id))
                    {
                        KiteChat.MultiDeepMarkovChains.Feed(msg);
                    }
                }
            });
            await KiteChat.MultiDeepMarkovChains.SaveAsync();//TODO: Fuck with this some more http://stackoverflow.com/questions/1930982/when-should-i-call-savechanges-when-creating-1000s-of-entity-framework-object
            await ReplyAsync($"{i*100} messages downloaded.");
        }
    }
}
