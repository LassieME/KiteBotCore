using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using ExtendedGiantBombClient.Interfaces;
using ExtendedGiantBombClient.Model;
using GiantBomb.Api;
using GiantBomb.Api.Model;
using KiteBotCore.Json.GiantBomb.Videos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace KiteBotCore.Modules.GiantBombModules
{
    public class VideoService
    {
        public Dictionary<int, Video> AllVideos { get; private set; }
        public bool IsReady { get; private set; }
        public static string JsonVideoFileLocation => Directory.GetCurrentDirectory() + "/Content/videos.json";
        private readonly JsonSerializerSettings _settings;
        private readonly IExtendedGiantBombRestClient _gbApiClient;

        public VideoService(IExtendedGiantBombRestClient gbClient)
        {
            _gbApiClient = gbClient;
            _settings = new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss",
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            _ = Task.Run(InitializeTask);
        }

        private async Task InitializeTask()
        {
            if (File.Exists(JsonVideoFileLocation))
            {
                AllVideos = JsonConvert.DeserializeObject<Dictionary<int, Video>>(File.ReadAllText(JsonVideoFileLocation), _settings);
                try
                {
                    IEnumerable<Video> latestPage = await _gbApiClient.GetVideosAsync().ConfigureAwait(false);
                    foreach (Video video in latestPage.Where(x => AllVideos.All(y => y.Key != x.Id)))
                    {
                        AllVideos[video.Id] = video;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, ex.Message);
                }
            }
            else
            {
                Log.Warning("Did not find videos.json, running full GB video list download");

                var latest = await _gbApiClient.GetAllVideosAsync().ConfigureAwait(false);
                AllVideos = new Dictionary<int, Video>(latest.Count() + 150);
                foreach (var video in latest)
                {
                    AllVideos[video.Id] = video;
                }
            }

            IsReady = true;
            File.WriteAllText(JsonVideoFileLocation, JsonConvert.SerializeObject(AllVideos, _settings));
        }
    }

    public static class VideoExtension
    {
        public static EmbedBuilder ToEmbed(this Video video)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder
                .WithTitle(video.Name)
                .WithUrl(video.SiteDetailUrl)
                .WithDescription(video.Deck ?? "No Deck on Giant Bomb.")
                .WithImageUrl(video.Image.SuperUrl) // ?? video.Image?.ScreenUrl ?? video.Image?.MediumUrl ?? video.Image?.SmallUrl)
                .WithAuthor(x => { x.Name = "Giant Bomb"; x.Url = "http://giantbomb.com/"; x.IconUrl = "http://giantbomb.com/favicon.ico"; })
                .WithColor(new Discord.Color(0x010101))
                .WithCurrentTimestamp();
            return embedBuilder;
        }
    }
}
