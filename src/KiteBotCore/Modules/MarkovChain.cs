using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;

namespace KiteBotCore.Modules
{
    public class MarkovChain : ModuleBase
    {
        [Command("testMarkov")]
        [Alias("tm")]
        [Summary("creates a Markov Chain string based on user messages")]
        [RequireServer(Server.KiteCo)]
        public async Task MarkovChainCommand()
        {
            await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence());
        }

        [Command("feed", RunMode = RunMode.Mixed)]
        [Summary("Downloads and feeds the markovchain")]
        [RequireOwner,RequireServer(Server.KiteCo)]
        public async Task FeedCommand(int amount)
        {
            var messages = Context.Channel.GetMessagesAsync(amount);
            int i = 0;
            await messages.ForEachAsync( m =>
            {
                i++;
                foreach (var msg in m)
                {
                    KiteChat.MultiDeepMarkovChains.Feed(msg);
                }                
            });
            await ReplyAsync($"{i*100} messages downloaded.");
        }
    }
}
