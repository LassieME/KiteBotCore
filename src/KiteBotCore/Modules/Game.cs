using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using KiteBotCore.Json.GiantBomb.Search;
using Newtonsoft.Json;

namespace KiteBotCore.Modules
{
    public class Game : ModuleBase
    {
        private static string _apiCallUrl;

        public Game(IDependencyMap map)
        {
            BotSettings botSettings;
            if (map.TryGet(out botSettings))
            {
                _apiCallUrl = $"http://www.giantbomb.com/api/search/?api_key={botSettings.GiantBombApiKey}&field_list=deck,image,name,original_release_date,platforms,site_detail_url&format=json&query=\"";
            }
        }

        [Command("game")]
        [Summary("Finds a game in the Giantbomb games database")]
        public async Task GameCommand([Remainder] string gameTitle)
        {
            if (!string.IsNullOrWhiteSpace(gameTitle))
            {
                var s = await GetGamesEndpoint(gameTitle, 0);
                await ReplyAsync(s.Results.FirstOrDefault()?.ToString());
            }
            else
            {
                await ReplyAsync("Empty game name given, please specify a game title");
            }
        }

        private async Task<Search> GetGamesEndpoint(string gameTitle, int retry)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<Search>(await client.GetStringAsync(_apiCallUrl+gameTitle+"\""));
                }
            }
            catch (Exception)
            {
                if (++retry < 3)
                {
                    await Task.Delay(10000);
                    return await GetGamesEndpoint(gameTitle, retry);
                }
                throw new TimeoutException();
            }
        }
    }
}