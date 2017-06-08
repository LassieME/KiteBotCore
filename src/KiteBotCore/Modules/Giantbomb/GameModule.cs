using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using KiteBotCore.Json.GiantBomb.GameResult;
using KiteBotCore.Json.GiantBomb.Search;
using KiteBotCore.Utils.FuzzyString;
using Newtonsoft.Json;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KiteBotCore.Modules
{
    public class GameModule : ModuleBase
    {
        public Random Rand { get; set; }
        public FollowUpService FollowUpService { get; set; }
        public BotSettings BotSettings { get; set; }
        private readonly IServiceProvider _services;
        private readonly string _searchAPIUrl;
        private readonly Func<int, string> _gameAPIUrl;
        private Stopwatch _stopwatch;

        protected override void BeforeExecute()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute()
        {
            _stopwatch.Stop();
            Log.Debug($"Game Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        public GameModule(IServiceProvider services)
        {
            _services = services;
            var botSettings = _services.GetService<BotSettings>();
            
            _searchAPIUrl =
                $"http://www.giantbomb.com/api/search/?api_key={botSettings.GiantBombApiKey}&resources=game&field_list=deck,image,name,original_release_date,platforms,site_detail_url,expected_release_day,expected_release_month,expected_release_quarter,expected_release_year&format=json&query=";
            _gameAPIUrl =
                (intId) =>
                $"http://www.giantbomb.com/api/game/3030-{intId}/?api_key={botSettings.GiantBombApiKey}&field_list=deck,expected_release_day,expected_release_month,expected_release_quarter,expected_release_year,image,name,original_release_date,platforms,site_detail_url&format=json";
            
        }

        [Command("game", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Finds a game in the Giantbomb games database")]
        public async Task GameCommand([Remainder] string gameTitle)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(gameTitle))
                {
                    var search = await GetSearchEndpoint(gameTitle, 3).ConfigureAwait(false);

                    if (search.Results.Length == 1)
                    {
                        await ReplyAsync("", embed: search.Results.FirstOrDefault().ToEmbed()).ConfigureAwait(false);
                    }
                    else if (search.Results.Length > 1)
                    {
                        var dict = new Dictionary<string, Tuple<string, Func<EmbedBuilder>>>();

                        int i = 1;
                        string reply = "Which of these games did you mean?" + Environment.NewLine;
                        foreach (var result in search.Results.OrderBy(x => x.Name.LevenshteinDistance(gameTitle)))
                        {
                            if (result.Name != null)
                            {
                                dict.Add(i.ToString(),
                                    Tuple.Create<string, Func<EmbedBuilder>>("", () => result.ToEmbed()));
                                reply += $"{i++}. {result.Name} {Environment.NewLine}";
                            }
                            else
                            {
                                break;
                            }
                        }
                        var messageToEdit =
                            await ReplyAsync(
                                    reply +
                                    "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.")
                                .ConfigureAwait(false);
                        FollowUpService.AddNewFollowUp(new FollowUp(_services, dict, Context.User.Id, Context.Channel.Id,
                            messageToEdit));
                    }
                    else
                    {
                        await ReplyAsync("Giant Bomb doesn\'t have any games that match that name.")
                            .ConfigureAwait(false);
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

        [Command("randomgame", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Replies with random game from the Giant Bomb games database")]
        public async Task RandomGameCommand()
        {
            int randomId = Rand.Next(1, 54250);
            GameResult result;
            try
            {
                result = await GetGameEndpoint(randomId, 3).ConfigureAwait(false);
            }
            catch (JsonSerializationException)
            {
                randomId = Rand.Next(1, 54250);
                result = await GetGameEndpoint(randomId, 3).ConfigureAwait(false);
            }
            await ReplyAsync("", embed: result.Results.ToEmbed()).ConfigureAwait(false);
        }

        private async Task<GameResult> GetGameEndpoint(int gameId, int retry)
        {
            await RateLimit.WaitAsync().ConfigureAwait(false);
            var rateLimitTask = StartRatelimit();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<GameResult>(await client.GetStringAsync(_gameAPIUrl(gameId))
                        .ConfigureAwait(false));
                }
            }
            catch (TimeoutException timeoutEx)
            {
                Log.Debug(timeoutEx, timeoutEx.Message);
                if (retry > 0)
                {
                    await Task.Delay(2000).ConfigureAwait(false);
                    await rateLimitTask.ConfigureAwait(false);
                    return await GetGameEndpoint(gameId, retry - 1).ConfigureAwait(false);
                }
                throw;
            }
        }
        
        private async Task<Search> GetSearchEndpoint(string gameTitle, int retry)
        {
            await RateLimit.WaitAsync().ConfigureAwait(false);
            var rateLimitTask = StartRatelimit();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<Search>(await client.GetStringAsync(Uri.EscapeUriString($@"{_searchAPIUrl}""{gameTitle}""")).ConfigureAwait(false));
                }
            }
            catch (Exception)
            {
                if (retry > 0)
                {
                    await Task.Delay(2000).ConfigureAwait(false);
                    await rateLimitTask.ConfigureAwait(false);
                    return await GetSearchEndpoint(gameTitle, retry - 1).ConfigureAwait(false);
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