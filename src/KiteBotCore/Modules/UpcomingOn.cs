using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using Newtonsoft.Json;

namespace KiteBotCore.Modules
{
    public class UpcomingOn : ModuleBase
    {
        public static string UpcomingUrl = "http://www.giantbomb.com/upcoming_json";

        [Command("upcoming")]
        [Summary("Lists upcoming content on Giant Bomb")]
        public async Task UpcomingCommand()
        {
            var json = await TestDownload();
            string output = "";
            output += json.LiveNow + "\n";
            output += string.Join("\n", json.Upcoming.ToList());
            await ReplyAsync(output);
        
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
