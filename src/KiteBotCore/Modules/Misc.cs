using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace KiteBotCore.Modules
{
    public class Misc : ModuleBase
    {
        public CommandHandler ch { get; set; }

        [Command("420")]
        [Alias("#420", "blaze", "waifu")]
        [Summary("Anime and weed, all you need.")]
        public async Task FourTwentyCommand()
        {
            await ReplyAsync("http://420.moe/", false, new EmbedBuilder().WithDescription("ANIME AND WEED ALL U NEED")).ConfigureAwait(false);
        }
        
        [Command("makemycommand", RunMode = RunMode.Async)]
        [Summary("Anime and weed, all you need.")]
        [RequireBotOwner]
        public async Task MakeMyCommand(string commandName, [Remainder]string input)
        {
            await ch.Commands.CreateModuleAsync(commandName, builder =>
            {
                builder.AddCommand("", async (context, objects, arg3, arg4) =>
                    {
                        await context.Channel.SendMessageAsync(input);
                    }, 
                    commandBuilder => 
                        commandBuilder
                            .WithRunMode(RunMode.Async)
                            .WithPriority(0));
            });
        }

        [Command("archive")]
        [Alias("gbarchive")]
        [Summary("Links you the GB livestream archive")]
        public async Task GBArchiveCommand()
        {
            await ReplyAsync("http://www.giantbomb.com/videos/embed/8635/?allow_gb=yes").ConfigureAwait(false);
        }

        [Command("hi", RunMode = RunMode.Async)]
        [Summary("Mentions you and says hello")]
        public async Task HiCommand()
        {
            await ReplyAsync($"{Context.User.Mention} Heyyo!").ConfigureAwait(false);
        }

        [Command("summerjams", RunMode = RunMode.Async)]
        [Alias("real summerjams", "realest summerjams")]
        [Summary("Ryan Davis summerjam playlist")]
        public async Task SummerJamsCommand()
        {
            await ReplyAsync(@"https://open.spotify.com/user/taswell/playlist/26eEZkxxU2KzKLmtVDF8he" + 
                "\n" +
                "https://play.google.com/music/playlist/AMaBXym9sy9twqdblyFDsN0Pp7hOgSiq7Efg_OlHcnzJJNT2aArwug17GN-jLDCGiNLHm7VfQ7PN8AvCx8PVb-LbrT8Jb4DKwA%3D%3D").ConfigureAwait(false);
        }

        [Command("randomql", RunMode = RunMode.Async)]
        [Alias("randql","randomquicklook")]
        [Summary("Posts a random quick look.")]
        [Ratelimit(2, 1, Measure.Minutes)]
        public async Task RandomQLCommand()
        {
            string url = "http://qlcrew.com/main.php?vid_type=ql&anyone=anyone&inc%5B0%5D=&exc%5B0%5D=&p=1&per_page=15&random";
            await ReplyAsync(await GetResponseUriFromRandomQlCrew(url).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Command("randomryan", RunMode = RunMode.Async)]
        [Alias("randomvideo ryan")]
        [Summary("Posts a random Ryan Davis video.")]
        [Ratelimit(2, 1, Measure.Minutes)]
        [RequireDateSpan("02/07/2017 00:00:01", "09/07/2017 23:59:59")]
        public async Task RandomRyanCommand()
        {
            string url = "http://qlcrew.com/main.php?vid_type=all&inc%5B0%5D=2&hlt=any&game_id=any&platforms=any&length=0&maxlen=inf&per_page=15&p=1&random";
            await ReplyAsync(await GetResponseUriFromRandomQlCrew(url).ConfigureAwait(false)).ConfigureAwait(false);
        }

        public async Task<string> GetResponseUriFromRandomQlCrew(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            if (request != null)
            {
                try
                {
                    HttpWebResponse response = await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
                    return response?.ResponseUri.AbsoluteUri;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex + ex.Message);
                }
            }
            return "Couldn't load QLcrew's Random Link.";
        }
    }
}