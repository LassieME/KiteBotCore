using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using KiteBotCore.Utils.FuzzyString;
using Discord;
using ExtendedGiantBombClient.Interfaces;
using GiantBomb.Api;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KiteBotCore.Modules.GiantBombModules
{
    public class GameModule : ModuleBase
    {
        public Random Rand { get; set; }
        public FollowUpService FollowUpService { get; set; }
        public IExtendedGiantBombRestClient GbClient { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        private Stopwatch _stopwatch;

        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"{command.Name} Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        //_searchAPIUrl = $"http://www.giantbomb.com/api/search/?api_key={botSettings.GiantBombApiKey}&resources=game&field_list=deck,image,name,original_release_date,platforms,site_detail_url,expected_release_day,expected_release_month,expected_release_quarter,expected_release_year&format=json&query=";
        //_gameAPIUrl = (intId) => $"http://www.giantbomb.com/api/game/3030-{intId}/?api_key={botSettings.GiantBombApiKey}&field_list=deck,expected_release_day,expected_release_month,expected_release_quarter,expected_release_year,image,name,original_release_date,platforms,site_detail_url&format=json";
            
        [Command("game", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Finds a game in the Giantbomb games database")]
        public async Task GameCommand([Remainder] string gameTitle)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(gameTitle))
                {
                    var search = (await GbClient.SearchForGamesAsync(gameTitle).ConfigureAwait(false)).ToList();

                    if (search.Count == 1)
                    {
                        await ReplyAsync("", embed: search.FirstOrDefault().ToEmbed()).ConfigureAwait(false);
                    }
                    else if (search.Count > 1)
                    {
                        var dict = new Dictionary<string, Tuple<string, Func<EmbedBuilder>>>();

                        int i = 1;
                        string reply = "Which of these games did you mean?" + Environment.NewLine;
                        foreach (var result in search.OrderBy(x => x.Name.LevenshteinDistance(gameTitle)).Take(10))
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
                        FollowUpService.AddNewFollowUp(new FollowUp(ServiceProvider, dict, Context.User.Id, Context.Channel.Id,
                            messageToEdit));
                    }
                    else
                    {
                        await ReplyAsync(@"Giant Bomb doesn't have any games that match that name.")
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
            GiantBomb.Api.Model.Game result = null;
            int error = 0;
            while (result == null && error < 5)
            {
                try
                {
                    var randomId = Rand.Next(1, 54500);
                    result = await GbClient.GetGameAsync(randomId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    error += 1;
                    Log.Debug(ex, ex.Message);
                }
            }
            await ReplyAsync("", embed: result.ToEmbed()).ConfigureAwait(false);
        }
    }

    public static class GameExtension
    {
        public static EmbedBuilder ToEmbed(this GiantBomb.Api.Model.Game game)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(game.Name)
                .WithUrl(game.SiteDetailUrl)
                .WithDescription(game.Deck ?? "No Deck")
                .WithImageUrl(game.Image?.MediumUrl ?? game.Image?.SmallUrl ?? game.Image?.ThumbUrl ?? game.Image?.SuperUrl ??
                              "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/200px-No_image_available.svg.png")
                .WithFooter(x => x.Text = "Giant Bomb")
                .WithColor(new Discord.Color(0x00CC00))
                .WithCurrentTimestamp();

            if (game.OriginalReleaseDate != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "First release date";
                    x.Value = game.OriginalReleaseDate?.ToString("dd MMMM yyyy");
                    x.IsInline = true;
                });
            }
            else if (game.ExpectedReleaseDay != null && game.ExpectedReleaseMonth != null && game.ExpectedReleaseYear != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "Expected release date";
                    x.Value =
                        $"{game.ExpectedReleaseYear}-{(game.ExpectedReleaseMonth < 10 ? "0" + game.ExpectedReleaseMonth : game.ExpectedReleaseMonth.ToString())}-{game.ExpectedReleaseDay}";
                    x.IsInline = true;
                });
            }
            else if (game.ExpectedReleaseQuarter != null && game.ExpectedReleaseYear != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "Expected release quarter";
                    x.Value = $"Q{game.ExpectedReleaseQuarter} {game.ExpectedReleaseYear}";
                    x.IsInline = true;
                });
            }

            if (game.Platforms != null && game.Platforms.Any())
                embedBuilder.AddField(x =>
                {
                    x.Name = "Platforms";
                    x.Value = game.Platforms != null ? string.Join(", ", game.Platforms?.Select(y => y.Name)) : null;
                    x.IsInline = true;
                });

            return embedBuilder;
        }
    }
}