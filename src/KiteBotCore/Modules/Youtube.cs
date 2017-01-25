using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;

namespace KiteBotCore.Modules
{
    public class Youtube : ModuleBase //TODO: Move to using groups when https://github.com/RogueException/Discord.Net/pull/482 is pulled
    {
        [Command("youtube")]
        [Summary("Lists youtube commands and how to use them.")]
        [RequireOwner]
        public async Task YoutubeCommand()
        {
            await ReplyAsync("Ask Lassie, he forgot to put something here.");
        }

        
        public class YoutubeLivestream : ModuleBase
        {
            [Command("youtube livestream")]
            [Summary("Lists youtube video commands and how to use them.")]
            [RequireOwner]
            public async Task LivestreamCommand()
            {
                await ReplyAsync("Ask Lassie, he forgot to put something here.");
            }

            [Command("youtube livestream add")]
            [Summary("Adds a youtube channel to start watching for new livestreams.")]
            [RequireOwner]
            public async Task AddCommand([Remainder]string channelUrl)
            {
                bool isValid = ValidateChannelId(channelUrl);
                await ReplyAsync(isValid ? "OK" : "That is not a valid channelId, please use http://johnnythetank.github.io/youtube-channel-name-converter/");
            }

            [Command("youtube livestream remove")]
            [Summary("Removes youtube channel from being watched for new livestreams.")]
            [RequireOwner]
            public async Task RemoveCommand([Remainder]string channelUrl)
            {
                await ReplyAsync("Ask Lassie, he forgot to put something here.");
            }
        }

        public class YoutubeVideo : ModuleBase
        {
            [Command("youtube video")]
            [Summary("Lists youtube video commands and how to use them.")]
            [RequireOwner]
            public async Task VideoCommand()
            {
                await ReplyAsync("Ask Lassie, he forgot to put something here.");
            }

            [Command("youtube video add")]
            [Summary("Adds a youtube channel to start watching for new videos.")]
            [RequireOwner]
            public async Task AddCommand([Remainder]string channelUrl)
            {
                bool isValid = ValidateChannelId(channelUrl);
                await ReplyAsync(isValid ? "OK" : "That is not a valid channelId, please use http://johnnythetank.github.io/youtube-channel-name-converter/");
            }

            [Command("youtube video remove")]
            [Summary("Removes a youtube channel from being watched for new videos.")]
            [RequireOwner]
            public async Task RemoveCommand([Remainder]string channelUrl)
            {
                await ReplyAsync("Ask Lassie, he forgot to put something here.");
            }
        }

        private static string _latestVideo;

        [Command("latestexample")]
        [Summary("posts a video if a new video exists since the last time this command was ran")]
        [RequireOwner]
        public async Task YoutubeTestCommand()
        {
            SearchResource.ListRequest listRequest = YoutubeModuleService.YoutubeS.Search.List("snippet");
            listRequest.ChannelId = "UCmeds0MLhjfkjD_5acPnFlQ";
            listRequest.Type = "video";
            listRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live;
            listRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            listRequest.MaxResults = 1;
            SearchListResponse searchResponse = listRequest.Execute();
            Google.Apis.YouTube.v3.Data.SearchResult data = searchResponse.Items.FirstOrDefault(v => v.Id.Kind == "youtube#video");

            string currentVideo = data.Id.VideoId;

            if (_latestVideo != currentVideo)
            {
                await ReplyAsync($"https://youtu.be/{data.Id.VideoId}");
            }
            _latestVideo = currentVideo;
        }

        private static bool ValidateChannelId(string channelId)
        {
            try
            {
                SearchResource.ListRequest listRequest = YoutubeModuleService.YoutubeS.Search.List("snippet");
                listRequest.ChannelId = channelId;
                listRequest.Type = "video";
                listRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                listRequest.MaxResults = 1;
                listRequest.Execute();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public static class YoutubeModuleService
    {
        public static YouTubeService YoutubeS;
        static YoutubeModuleService()
        {
            YoutubeS = new YouTubeService(new Google.Apis.Services.BaseClientService.Initializer { ApiKey = "" });
        }
    }
}
