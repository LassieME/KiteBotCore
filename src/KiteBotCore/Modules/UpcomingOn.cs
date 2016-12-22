using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using Newtonsoft.Json;
using Discord;

namespace KiteBotCore.Modules
{
    public class UpcomingOn : ModuleBase
    {
        public static string UpcomingUrl = "http://www.giantbomb.com/upcoming_json";

        //[Command("upcoming")]
        //[Summary("Lists upcoming content on Giant Bomb")]
        //public async Task UpcomingCommand()
        //{
        //    var json = await TestDownload();
        //    string output = "";
        //    output += json.LiveNow + "\n";
        //    output += string.Join("\n", json.Upcoming.ToList());
        //    await ReplyAsync(output);        
        //}

        [Command("upcoming")]
        [Summary("Lists upcoming content on Giant Bomb")]
        public async Task UpcomingNewCommand()
        {
            var json = await TestDownload();
            string output = "";
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Upcoming on Giant Bomb")
                .WithDescription("All dates are in PST")
                .WithColor(new Color(0x660066))
                .WithAuthor(x =>
                {
                    x.Name = "Giant Bomb";
                    x.Url = "http://giantbomb.com/";
                    x.IconUrl = "http://giantbomb.com/favicon.ico";
                })
                .WithThumbnailUrl("http://www.giantbomb.com/bundles/phoenixsite/images/core/loose/logo-gb-midsize.png");              
            foreach (var upcoming in json.Upcoming)
            {
                embed.AddField(x =>
                {
                    x.Name = $"{(upcoming.Premium ? "Premium " + upcoming.Type : upcoming.Type)}: {upcoming.Title}";
                    x.Value = $"on {upcoming.Date}";
                });
            }
            await ReplyAsync(output, false, embed);
        }        

        internal static async Task<GbUpcoming> TestDownload()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "KiteBotCore 1.1 GB Discord Bot looking for upcoming content");
                GbUpcoming json = JsonConvert.DeserializeObject<GbUpcoming>(await client.GetStringAsync(UpcomingUrl));
                return json;
            }
        }
    }
}
