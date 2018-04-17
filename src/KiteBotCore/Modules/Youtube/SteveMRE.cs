using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Apis.YouTube.v3;
using Serilog;

namespace KiteBotCore.Modules.Youtube
{
    public class SteveMREModule : ModuleBase
    {
        public YouTubeService YouTubeService { get; set; }
        public Random Random { get; set; }

        public static List<Google.Apis.YouTube.v3.Data.SearchResult> SearchResults = new List<Google.Apis.YouTube.v3.Data.SearchResult>(200);

        [Command("SteveMRE", RunMode = RunMode.Async)]
        [Alias("MRE")]
        public async Task SteveMreCommand()
        {
            if (SearchResults.Count == 0)
            {
                SearchResource.ListRequest listRequest = YouTubeService.Search.List("snippet");
                listRequest.ChannelId = "UC2I6Et1JkidnnbWgJFiMeHA";
                listRequest.Type = "video";
                listRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                listRequest.MaxResults = 50;

                var result = await listRequest.ExecuteAsync();

                Log.Information($"MRECommand found {result.PageInfo.TotalResults} videos, paginating");
                SearchResults.AddRange(result.Items);
                var nextPageToken = result.NextPageToken;
                while (nextPageToken != null)
                {
                    listRequest.PageToken = nextPageToken;
                    result = await listRequest.ExecuteAsync();

                    SearchResults.AddRange(result.Items);
                    nextPageToken = result.NextPageToken;
                }
            }
            await ReplyAsync($"{@"https://www.youtube.com/watch?v="}{SearchResults[Random.Next(0,SearchResults.Count)].Id.VideoId}");
        }
    }
}
