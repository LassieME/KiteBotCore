using System.Threading.Tasks;
using Discord.Commands;

namespace KiteBotCore.Modules
{
    public class Misc : ModuleBase
    {
        [Command("420")]
        [Alias("#420", "blaze", "waifu")]
        [Summary("Anime and weed, all you need.")]
        public async Task FourTwentyCommand()
        {
            await ReplyAsync("http://420.moe/");
        }

        [Command("randomql")]
        [Alias("randql")]
        [Summary("Posts a random quick look.")]
        public async Task Command()
        {
            await ReplyAsync(KiteChat.GetResponseUriFromRandomQlCrew());
        }
    }
}