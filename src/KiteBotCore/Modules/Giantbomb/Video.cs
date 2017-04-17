using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.Videos;
using KiteBotCore.Utils.FuzzyString;

namespace KiteBotCore.Modules.Giantbomb
{
    public class VideoModule : ModuleBase
    {
        public IDependencyMap Map { get; set; }
        public VideoService VideoService { get; set; }
        public Random Random { get; set; }

        [Command("video", RunMode = RunMode.Async), Ratelimit(2, 1, Measure.Minutes)]
        [Summary("Searches through GB videos for the 5 closest matches to the query")]
        public async Task VideoCommand([Remainder] string videoTitle)
        {
            if (!VideoService.IsReady)
            {
                await ReplyAsync("Bot has not finished downloading all videos yet.");
                return;
            }
            if (string.IsNullOrWhiteSpace(videoTitle))
            {
                await ReplyAsync("Empty video title given, please specify");
                return;
            }
            var dict = new Dictionary<string, Tuple<string, EmbedBuilder>>();
            int i = 1;

            string reply = "Which of these videos did you mean?" + Environment.NewLine;

            string videoTitleToLower = videoTitle.ToLower();
            foreach (Result video in VideoService.AllVideos.Values
                .OrderByDescending(x => x.Name.ToLower().LongestCommonSubstring(videoTitleToLower).Length).Take(20)
                .OrderBy(x => x.Name.LevenshteinDistance(videoTitle)).Take(10))
            {
                dict.Add(i.ToString(), Tuple.Create("", video.ToEmbed()));
                reply += $"{i++}. {video.Name} {Environment.NewLine}";
            }
            var messageToEdit =
                await ReplyAsync(reply +
                                 "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.");
            FollowUpService.AddNewFollowUp(new FollowUp(Map, dict, Context.User.Id, Context.Channel.Id,
                messageToEdit));
        }

        [Command("randomvideo", RunMode = RunMode.Async), Ratelimit(4, 1, Measure.Minutes)]
        [Summary("Posts a truly random video from Giant Bomb")]
        public async Task RandomVideoCommand()
        {
            if (!VideoService.IsReady)
            {
                await ReplyAsync("Bot has not finished downloading all videos yet.");
                return;
            }
            
            await ReplyAsync(VideoService.AllVideos.Values.ToArray()[Random.Next(VideoService.AllVideos.Count)].SiteDetailUrl);
        }
    }
}
