using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using Discord;
using Serilog;

namespace KiteBotCore.Modules.Youtube
{
    [Group("youtube")]
    public class Youtube : ModuleBase 
    {
        [Command]
        [Summary("Lists youtube commands and how to use them.")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
        public async Task YoutubeCommand()
        {
            await ReplyAsync("Use this command + `list` to list youtube channels that are currently subscribed to in this channel, or use this command + `video` or `livestream` + `add` or `remove`").ConfigureAwait(false);
        }

        [Command("list")]
        [Summary("Lists youtube channels subscribed to in this channel.")]
        [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
        public async Task YoutubeListCommand()
        {
            await ReplyAsync(YoutubeModuleService.List(Context.Channel.Id)).ConfigureAwait(false);
        }

        [Group("livestream")]
        public class YoutubeLivestream : ModuleBase
        {
            [Command]
            [Summary("Lists youtube video commands and how to use them.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task LivestreamCommand()
            {
                await ReplyAsync("Use this command + `add` or `remove` to subscribe or unsubscribe a channel from posting new content in the discord channel the command is ran in.").ConfigureAwait(false);
            }

            [Command("add")]
            [Summary("Adds a youtube channel to start watching for new livestreams.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task AddCommand([Remainder]string channelUrl)
            {
                (bool, string) isValidAndChannelName = ValidateChannelId(channelUrl);
                var msg = await ReplyAsync(isValidAndChannelName.Item1 ? "OK" : "That is not a valid channelId, please use http://johnnythetank.github.io/youtube-channel-name-converter/").ConfigureAwait(false);
                bool wasAdded = YoutubeModuleService.TryAdd(Context.Channel.Id, channelUrl, isValidAndChannelName.Item2, WatchType.Livestream);
                await msg.ModifyAsync(x => x.Content = wasAdded ? $"OK, {isValidAndChannelName.Item2} added to Livestream-watchlist." : "OK, Channel already added.").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes youtube channel from being watched for new livestreams.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task RemoveCommand([Remainder]string channelUrl)
            {
                bool wasAdded = YoutubeModuleService.TryRemove(Context.Channel.Id, channelUrl, WatchType.Livestream);
                await ReplyAsync(wasAdded ? "OK, Channel removed." : "Channel is not subscribed to this channel.").ConfigureAwait(false);
            }
        }

        [Group("video")]
        public class YoutubeVideo : ModuleBase
        {
            [Command]
            [Summary("Lists youtube video commands and how to use them.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task VideoCommand()
            {
                await ReplyAsync("Use this command + \"add\" or \"remove\" to subscribe or unsubscribe a channel from posting new content in the discord channel the command is ran in.").ConfigureAwait(false);
            }

            [Command("add")]
            [Summary("Adds a youtube channel to start watching for new videos.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task AddCommand([Remainder]string channelUrl)
            {
                (bool, string) isValidAndChannelName = ValidateChannelId(channelUrl);
                var msg = await ReplyAsync(isValidAndChannelName.Item1 ? "OK" : "That is not a valid channelId, please use http://johnnythetank.github.io/youtube-channel-name-converter/").ConfigureAwait(false);
                bool wasAdded = YoutubeModuleService.TryAdd(Context.Channel.Id, channelUrl, isValidAndChannelName.Item2, WatchType.Video);
                await msg.ModifyAsync(x => x.Content = wasAdded ? $"OK, {isValidAndChannelName.Item2} added to Video-watchlist." : "OK, Channel already added.").ConfigureAwait(false);
            }

            [Command("remove")]
            [Summary("Removes a youtube channel from being watched for new videos.")]
            [RequireOwnerOrUserPermission(GuildPermission.ManageGuild)]
            public async Task RemoveCommand([Remainder]string channelUrl)
            {
                bool wasAdded = YoutubeModuleService.TryRemove(Context.Channel.Id, channelUrl, WatchType.Video);
                await ReplyAsync(wasAdded ? "OK, Channel removed." : "Channel is not subscribed to this channel.").ConfigureAwait(false);
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
                await ReplyAsync($"https://youtu.be/{data.Id.VideoId}").ConfigureAwait(false);
            }
            _latestVideo = currentVideo;
        }

        private static (bool, string) ValidateChannelId(string channelId)
        {
            try
            {
                SearchResource.ListRequest listRequest = YoutubeModuleService.YoutubeS.Search.List("snippet");
                listRequest.ChannelId = channelId;
                listRequest.Type = "video";
                listRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                listRequest.MaxResults = 1;
                var request = listRequest.Execute();
                return (true, request.Items.FirstOrDefault().Snippet.ChannelTitle);
            }
            catch (Exception ex)
            {
                Log.Verbose(ex + ex.Message);
                return (false, null);
            }
        }
    }
}
