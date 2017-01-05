using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using KiteBotCore.Json.GiantBomb.Search;
using KiteBotCore.Utils.FuzzyString;
using Newtonsoft.Json;
using Discord;

namespace KiteBotCore.Modules
{
    public class Game : ModuleBase
    {
        private static string _apiCallUrl;
        private readonly IDependencyMap _map;

        public Game(IDependencyMap map)
        {
            _map = map;
            
            if (_map.TryGet(out BotSettings botSettings))
            {
                _apiCallUrl = $"http://www.giantbomb.com/api/search/?api_key={botSettings.GiantBombApiKey}&resources=game&field_list=deck,image,name,original_release_date,platforms,site_detail_url&format=json&query=";
            }
        }

        [Command("game")]
        [Summary("Finds a game in the Giantbomb games database")]
        public async Task GameCommand([Remainder] string gameTitle)
        {
            if (!string.IsNullOrWhiteSpace(gameTitle))
            {
                var search = await GetGamesEndpoint(gameTitle, 0);
                
                if (search.Results.Length == 1)
                {
                    await ReplyAsync("", embed: search.Results.FirstOrDefault().ToEmbed());
                }
                else if (search.Results.Length > 1)
                {
                    var dict = new Dictionary<string, Tuple<string, EmbedBuilder>>();

                    int i = 1;
                    string reply = "Which of these games did you mean?" + Environment.NewLine;
                    foreach (var result in search.Results.OrderBy(x => x.Name.LevenshteinDistance(gameTitle)).Take(10))
                    {
                        if (result.Name != null)
                        {
                            dict.Add(i.ToString(), Tuple.Create("", result.ToEmbed()));
                            reply += $"{i++}. {result.Name} {Environment.NewLine}";
                        }
                        else
                        {
                            break;
                        }
                    }
                    var messageToEdit = await ReplyAsync(reply + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.");
                    FollowUpService.AddNewFollowUp(new FollowUp(_map, dict, Context.User.Id, Context.Channel.Id, messageToEdit));
                }
                else
                {
                    await ReplyAsync("Giantbomb doesn\'t have any games that match that name.");
                }
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
                    await Task.Delay(1000);
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<Search>(await client.GetStringAsync(Uri.EscapeUriString($@"{_apiCallUrl}""{gameTitle}""")));
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