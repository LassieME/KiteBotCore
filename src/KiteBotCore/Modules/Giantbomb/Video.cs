using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.Videos;
using KiteBotCore.Utils.FuzzyString;
using Serilog;

namespace KiteBotCore.Modules.Giantbomb
{
    public class VideoModule : ModuleBase
    {
        public IServiceProvider Services { get; set; }
        public FollowUpService FollowUpService { get; set; }
        public VideoService VideoService { get; set; }
        public Random Random { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Video Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("video", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Searches through GB videos for the 5 closest matches to the query")]
        public async Task VideoCommand([Remainder] string videoTitle)
        {
            if (!VideoService.IsReady)
            {
                await ReplyAsync("Bot has not finished downloading all videos yet.").ConfigureAwait(false);
                return;
            }
            if (string.IsNullOrWhiteSpace(videoTitle))
            {
                await ReplyAsync("Empty video title given, please specify").ConfigureAwait(false);
                return;
            }
            var dict = new Dictionary<string, Tuple<string, Func<EmbedBuilder>>>();
            int i = 1;

            string reply = "Which of these videos did you mean?" + Environment.NewLine;

            string videoTitleToLower = videoTitle.ToLower();
            foreach (Result video in VideoService.AllVideos.Values
                .OrderByDescending(x => x.Name.ToLower().LongestCommonSubstring(videoTitleToLower).Length).Take(20)
                .OrderBy(x => x.Name.LevenshteinDistance(videoTitle)).Take(10))
            {
                dict.Add(i.ToString(), Tuple.Create<string, Func<EmbedBuilder>>("", () => video.ToEmbed()));
                reply += $"{i++}. {video.Name} {Environment.NewLine}";
            }
            var messageToEdit =
                await ReplyAsync(reply +
                                 "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.").ConfigureAwait(false);
            FollowUpService.AddNewFollowUp(new FollowUp(Services, dict, Context.User.Id, Context.Channel.Id,
                messageToEdit));
        }

        [Command("randomvideo", RunMode = RunMode.Async), Ratelimit(4, 1, Measure.Minutes)]
        [Summary("Posts a truly random video from Giant Bomb")]
        public async Task RandomVideoCommand()
        {
            if (!VideoService.IsReady)
            {
                await ReplyAsync("Bot has not finished downloading all videos yet.").ConfigureAwait(false);
                return;
            }
            
            await ReplyAsync(VideoService.AllVideos.Values.ToArray()[Random.Next(VideoService.AllVideos.Count)].SiteDetailUrl).ConfigureAwait(false);
        }
    }
}
