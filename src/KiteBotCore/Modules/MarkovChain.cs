using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public class MarkovChain : ModuleBase
    {
        [Command("testMarkov")]
        [Alias("tm")]
        [Summary("creates a Markov Chain string based on user messages")]
        public async Task MarkovChainCommand()
        {
            await ReplyAsync(KiteChat.MultiDeepMarkovChains.GetSequence());
        }
    }
}
