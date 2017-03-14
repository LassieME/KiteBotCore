using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules
{
    public class Misc : ModuleBase
    {
        [Command("420")]
        [Alias("#420", "blaze", "waifu")]
        [Summary("Anime and weed, all you need.")]
        public async Task FourTwentyCommand()
        {
            await ReplyAsync("http://420.moe/").ConfigureAwait(false);
        }

        [Command("hi")]
        [Summary("Mentions you and says hello")]
        public async Task HiCommand()
        {
            await ReplyAsync($"{Context.User.Mention} Heyyo!").ConfigureAwait(false);
        }

        [Command("randomql")]
        [Alias("randql")]
        [Summary("Posts a random quick look.")]
        public async Task Command()
        {
            await ReplyAsync(await KiteChat.GetResponseUriFromRandomQlCrew().ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}