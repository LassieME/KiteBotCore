using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.Videos;
using KiteBotCore.Utils.FuzzyString;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore.Modules.Giantbomb
{
    public class Video : ModuleBase
    {
        private static Dictionary<int, Result> _allVideos;
        private static string _apiCallUrl;
        private readonly IDependencyMap _map;
        private static bool isReady = false;

        public static string JsonVideoFileLocation => Directory.GetCurrentDirectory() + "/Content/videos.json";

        Video(IDependencyMap map)
        {
            _map = map;
            //Check once for new videos, maybe just make this a timer.
        }

        internal static async Task InitializeTask(string apiKey)
        {
            _apiCallUrl = $"http://www.giantbomb.com/api/videos/?api_key={apiKey}&format=json";
            if (File.Exists(JsonVideoFileLocation))
            {
                _allVideos = JsonConvert.DeserializeObject<Dictionary<int, Result>>(File.ReadAllText(JsonVideoFileLocation));
                Videos latest = await GetVideosEndpoint(0, 3);
                foreach (Result result in latest.Results.Where(x => _allVideos.All(y => y.Key != x.Id)))
                {
                    _allVideos[result.Id] = result;
                }
            }
            else
            {
                Log.Debug("Running full GB video list download");
                _allVideos = new Dictionary<int, Result>(12500);
                Videos latest;
                int i = 0;
                do
                {
                    latest = await GetVideosEndpoint(i, 3);
                    Log.Verbose("Queried GB videos API {one}/{two}", latest.NumberOfPageResults + i*100, latest.NumberOfTotalResults);
                    foreach (var latestResult in latest.Results)
                    {
                        _allVideos[latestResult.Id] = latestResult;
                    }
                    i += 100;
                } while (latest.NumberOfPageResults == latest.Limit);
            }
            isReady = true;
            File.WriteAllText(JsonVideoFileLocation, JsonConvert.SerializeObject(_allVideos));
        }

        [Command("video", RunMode = RunMode.Mixed)]
        [Summary("Searches through GB videos for the 5 closest matches to the query")]
        public async Task VideoCommand([Remainder] string videoTitle)
        {
            if (!isReady)
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
            //foreach (
            //    Result video in _allVideos.Values.OrderBy(x => x.Name.LevenshteinDistance(videoTitle)).Take(5))
            //{
            //    dict.Add(i.ToString(), Tuple.Create("", video.ToEmbed()));
            //    reply += $"{i++}. {video.Name} {Environment.NewLine}";
            //}
            string videoTitleToLower = videoTitle.ToLower();
            foreach (
                Result video in _allVideos.Values.OrderByDescending(x => x.Name.ToLower().LongestCommonSubstring(videoTitleToLower).Length).Take(20).OrderBy(x => x.Name.LevenshteinDistance(videoTitle)).Take(10))
            {
                dict.Add(i.ToString(), Tuple.Create("", video.ToEmbed()));
                reply += $"{i++}. {video.Name} {Environment.NewLine}";
            }
            var messageToEdit =
                await ReplyAsync(reply +
                                 "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.");
            FollowUpService.AddNewFollowUp(new FollowUp(_map, dict, Context.User.Id, Context.Channel.Id,
                messageToEdit));
        }

        private static async Task<Videos> GetVideosEndpoint(int offset, int retry)
        {
            await RateLimit.WaitAsync();
            var rateLimitTask = StartRatelimit();
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot for fetching wiki information");
                    return JsonConvert.DeserializeObject<Videos>(await client.GetStringAsync($@"{_apiCallUrl}&offset={offset}"));
                }
            }
            catch (Exception)
            {
                if (++retry < 3)
                {
                    await Task.Delay(5000);
                    await rateLimitTask;
                    return await GetVideosEndpoint(offset, retry);
                }
                throw new TimeoutException();
            }
        }

        private static readonly SemaphoreSlim RateLimit = new SemaphoreSlim(1, 1);
        private static async Task StartRatelimit()
        {
            await Task.Delay(1000);
            RateLimit.Release();
        }
    }
}
