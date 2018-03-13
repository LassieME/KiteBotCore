using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore.Modules.Youtube
{
    internal static class YoutubeModuleService
    {
        internal static YouTubeService YoutubeS;
        private static bool _initialized;
        private static string WatchVideoPath => Directory.GetCurrentDirectory() + "/Content/WatchedVideoChannels.json";
        private static string WatchLivestreamPath => Directory.GetCurrentDirectory() + "/Content/WatchedLivestreamChannels.json";

        private static readonly Dictionary<string, WatchedChannel> VideoChannels = File.Exists(WatchVideoPath) 
            ? JsonConvert.DeserializeObject<Dictionary<string, WatchedChannel>>(File.ReadAllText(WatchVideoPath))
            : new Dictionary<string, WatchedChannel>();

        private static readonly Dictionary<string, WatchedChannel> LivestreamChannels = File.Exists(WatchLivestreamPath) 
            ? JsonConvert.DeserializeObject<Dictionary<string, WatchedChannel>>(File.ReadAllText(WatchLivestreamPath))
            : new Dictionary<string, WatchedChannel>();

        private static Queue<WatchedChannel> _queue = new Queue<WatchedChannel>(60);
        private static Timer _timer;
        private static DiscordSocketClient _client;
       

        internal static void Init(string apiKey, DiscordSocketClient client)
        {
            YoutubeS = new YouTubeService(new Google.Apis.Services.BaseClientService.Initializer { ApiKey = apiKey });
            _client = client;
            _initialized = true;

            int numberOfWatched = VideoChannels.Count + LivestreamChannels.Count;
            if (numberOfWatched > 0)
            {
                _timer = new Timer(RunSomethingAsync, null, 120000 / numberOfWatched, 120000 / numberOfWatched);
                foreach (var watched in VideoChannels.Values.Concat(LivestreamChannels.Values))
                {
                    _queue.Enqueue(watched);
                }
            }
        }

        private static async void RunSomethingAsync(object state)
        {
            var watched = _queue.Dequeue();
            _queue.Enqueue(watched);
            Log.Verbose("YoutubeChecker just ran.");
            await CheckForNewContentAsync(watched).ConfigureAwait(false);
        }

        private static async Task CheckForNewContentAsync(WatchedChannel watched)
        {
            SearchResource.ListRequest listRequest = YoutubeS.Search.List("snippet");
            listRequest.ChannelId = watched.ChannelId;
            listRequest.Type = "video";
            listRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            listRequest.MaxResults = 1;
            if(watched.WatchedType == WatchType.Livestream)
                listRequest.EventType = SearchResource.ListRequest.EventTypeEnum.Live;

            SearchListResponse searchResponse;
            try
            {
                searchResponse = await listRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "YoutubeService threw an error");
                return;
            }

            SearchResult data = searchResponse.Items.FirstOrDefault(v => v.Id.Kind == "youtube#video");

            if (data != null && (watched.LastVideoId != null || watched.WatchedType == WatchType.Livestream) && data.Id.VideoId != watched.LastVideoId)
            {
                foreach (var channelId in watched.ChannelsThatAreSubbed)
                {
                    if (_client.GetChannel(channelId) is SocketTextChannel socketTextChannel)
                    {
                        var eb = new EmbedBuilder()
                            .WithTitle(data.Snippet.Title)
                            .WithDescription($"{data.Snippet.ChannelTitle} {(watched.WatchedType == WatchType.Livestream ? "just went live!" : "just uploaded a new video!" )}")
                            .WithUrl($"https://www.youtube.com/watch?v=" + $"{data.Id.VideoId}")
                            .WithImageUrl(data.Snippet.Thumbnails.High.Url)
                            .WithColor(new Color(0xb31217));
                        await socketTextChannel.SendMessageAsync("", embed: eb);
                    }
                    else //means we're no longer in the guild that contains this channel, or the channel was deleted
                    {
                        TryRemove(channelId, watched.ChannelId, watched.WatchedType);
                    }
                }
            }
            watched.LastVideoId = data?.Id.VideoId;
        }

        private static void AddToQueue(WatchedChannel wc)
        {
            _queue.Enqueue(wc);
            if (_timer != null)
            {
                _timer.Change(120000 / _queue.Count, 120000 / _queue.Count);
            }
            else
            {
                _timer = new Timer(RunSomethingAsync, null, 120000 / _queue.Count, 120000 / _queue.Count);
            }
        }

        private static void RemoveFromQueue(WatchedChannel wc)
        {
            _queue = new Queue<WatchedChannel>(_queue.Where(s => s != wc));
            if (_queue.Count > 0)
            {
                _timer.Change(120000 / _queue.Count, 120000 / _queue.Count);
            }
            else
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        internal static bool TryAdd(ulong discordChannelId, string youtubeChannelId, string channelName, WatchType wType)
        {
            if (!_initialized) return false;
            bool wasAdded;
            switch (wType)
            {
                case WatchType.Livestream:
                    wasAdded = Add(LivestreamChannels, discordChannelId, youtubeChannelId, channelName, wType);
                    if (wasAdded)
                        File.WriteAllText(WatchLivestreamPath, JsonConvert.SerializeObject(LivestreamChannels));
                    return wasAdded;
                case WatchType.Video:
                    wasAdded = Add(VideoChannels, discordChannelId, youtubeChannelId, channelName, wType);
                    if (wasAdded)
                        File.WriteAllText(WatchVideoPath, JsonConvert.SerializeObject(VideoChannels));
                    return wasAdded;
            }
            return false;
        }

        private static bool Add(Dictionary<string, WatchedChannel> dict, ulong discordChannelId, string youtubeChannelId, string channelName, WatchType wType)
        {
            WatchedChannel wc;
            if (dict.ContainsKey(youtubeChannelId))
            {
                if (dict[youtubeChannelId].ChannelsThatAreSubbed.Contains(discordChannelId)) return false;
                dict[youtubeChannelId].ChannelsThatAreSubbed.Add(discordChannelId);
                return true;
            }
            dict.Add(youtubeChannelId, wc = new WatchedChannel
            {
                ChannelId = youtubeChannelId,
                ChannelName = channelName,
                ChannelsThatAreSubbed = new List<ulong> { discordChannelId },
                WatchedType = wType
            });
            AddToQueue(wc);
            return true;
        }

        internal static bool TryRemove(ulong discordChannelId, string channelIdOrName, WatchType wType)
        {
            if (!_initialized) return false;
            switch (wType)
            {
                case WatchType.Livestream:
                    return Remove(LivestreamChannels, discordChannelId, channelIdOrName);
                case WatchType.Video:
                    return Remove(VideoChannels, discordChannelId, channelIdOrName);
            }
            return false;
        }
        private static bool Remove(Dictionary<string, WatchedChannel> dict, ulong discordChannelId, string channelIdOrName)
        {
            if (dict.TryGetValue(channelIdOrName, out var wc))
            {
                if (wc.ChannelsThatAreSubbed.Any(x => x != discordChannelId))
                {
                    wc.ChannelsThatAreSubbed.Remove(discordChannelId);
                    return true;
                }
                dict.Remove(wc.ChannelId);
                RemoveFromQueue(wc);
                return true;
            }
            foreach (var watched in dict.Values)
            {
                if (watched.ChannelName != channelIdOrName) continue;
                if (watched.ChannelsThatAreSubbed.Any(x => x != discordChannelId))
                {
                    watched.ChannelsThatAreSubbed.Remove(discordChannelId);
                    return true;
                }
                dict.Remove(watched.ChannelId);
                RemoveFromQueue(watched);
                return true;
            }
            return false;
        }

        internal static string List(ulong discordChannelId)
        {
            var videos = VideoChannels.Where(x => x.Value.ChannelsThatAreSubbed.Contains(discordChannelId));
            var livestreams = LivestreamChannels.Where(x => x.Value.ChannelsThatAreSubbed.Contains(discordChannelId));
            var sb = new StringBuilder();

            sb.AppendLine("Channels checked for videos:");
            foreach (var channel in videos)
            {
                sb.AppendLine($"\t{channel.Value.ChannelName}");
            }

            sb.AppendLine();

            sb.AppendLine("Channels checked for livestreams:");
            foreach (var channel in livestreams)
            {
                sb.AppendLine($"\t{channel.Value.ChannelName}");
            }

            return sb.ToString();
        }
    }

    public class WatchedChannel
    {
        public WatchType WatchedType;
        public string ChannelId;
        public string ChannelName;
        public List<ulong> ChannelsThatAreSubbed;

        public bool WasStreamRunning = false;
        public string LastVideoId;
    }

    public enum WatchType
    {
        Livestream = 1,
        Video = 2
    }
}
