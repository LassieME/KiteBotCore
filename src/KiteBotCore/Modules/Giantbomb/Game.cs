using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using KiteBotCore.Json.GiantBomb.Search;
using KiteBotCore.Utils.FuzzyString;
using Newtonsoft.Json;
using Discord;
using Serilog;

namespace KiteBotCore.Modules
{
    public class Game : ModuleBase
    {
        private readonly string _apiCallUrl;
        private readonly IDependencyMap _map;

        public Game(IDependencyMap map)
        {
            _map = map;
            
            if (_map.TryGet(out BotSettings botSettings))
            {
                _apiCallUrl = $"http://www.giantbomb.com/api/search/?api_key={botSettings.GiantBombApiKey}&resources=game&field_list=deck,image,name,original_release_date,platforms,site_detail_url,expected_release_day,expected_release_month,expected_release_quarter,expected_release_year&format=json&query=";
            }
        }

        [Command("game", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Finds a game in the Giantbomb games database")]
        public async Task GameCommand([Remainder] string gameTitle)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(gameTitle))
                {
                    var search = await GetGamesEndpoint(gameTitle, 0).ConfigureAwait(false);

                    if (search.Results.Length == 1)
                    {
                        await ReplyAsync("", embed: search.Results.FirstOrDefault().ToEmbed()).ConfigureAwait(false);
                    }
                    else if (search.Results.Length > 1)
                    {
                        var dict = new Dictionary<string, Tuple<string, EmbedBuilder>>();

                        int i = 1;
                        string reply = "Which of these games did you mean?" + Environment.NewLine;
                        foreach (var result in search.Results.OrderBy(x => x.Name.LevenshteinDistance(gameTitle)))
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
                        var messageToEdit =
                            await ReplyAsync(reply + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.").ConfigureAwait(false);
                        FollowUpService.AddNewFollowUp(new FollowUp(_map, dict, Context.User.Id, Context.Channel.Id,
                            messageToEdit));
                    }
                    else
                    {
                        await ReplyAsync("Giantbomb doesn\'t have any games that match that name.").ConfigureAwait(false);
                    }
                }
                else
                {
                    await ReplyAsync("Empty game name given, please specify a game title").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex + ex.Message);
            }
        }

        private async Task<Search> GetGamesEndpoint(string gameTitle, int retry)
        {
            await RateLimit.WaitAsync().ConfigureAwait(false);
            var rateLimitTask = StartRatelimit();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<Search>(await client.GetStringAsync(Uri.EscapeUriString($@"{_apiCallUrl}""{gameTitle}""")).ConfigureAwait(false));
                }
            }
            catch (Exception)
            {
                if (++retry < 3)
                {
                    await Task.Delay(5000).ConfigureAwait(false);
                    await rateLimitTask.ConfigureAwait(false);
                    return await GetGamesEndpoint(gameTitle, retry).ConfigureAwait(false);
                }
                throw new TimeoutException();
            }
        }

        private static readonly SemaphoreSlim RateLimit = new SemaphoreSlim(1, 1);
        private static async Task StartRatelimit()
        {
            await Task.Delay(1000).ConfigureAwait(false);
            RateLimit.Release();
        }
    }
}