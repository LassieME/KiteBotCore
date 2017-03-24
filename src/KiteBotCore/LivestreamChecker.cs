using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json.GiantBomb.Chats;
using KiteBotCore.Modules;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore
{
    public class LivestreamChecker
    {
        public string ApiCallUrl;
        public int RefreshRate;
        private Timer _chatTimer;//Garbage collection doesnt like local timers.
        private Chats _latestPromo;
        private bool _wasStreamRunning;

        private static readonly DiscordSocketClient Client = Program.Client;
        private static string IgnoreFilePath => Directory.GetCurrentDirectory() + "/Content/ignoredChannels.json";
        private static readonly List<string> IgnoreList = File.Exists(IgnoreFilePath) ? 
            JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(IgnoreFilePath))
            : new List<string>();

        public LivestreamChecker(string gBapi, int streamRefresh, bool silentStartup)
        {
            if (gBapi.Length > 0)
            {
                ApiCallUrl = $"http://www.giantbomb.com/api/chats/?api_key={gBapi}&format=json";
                RefreshRate = streamRefresh;
                _wasStreamRunning = silentStartup;
                _chatTimer = new Timer(RefreshChatsApi, null, 60000, RefreshRate);
            }
        }

        public void Restart()
        {
            if (_chatTimer == null)
            {
                Console.WriteLine("_chatTimer eaten by GC");
                _chatTimer = new Timer(RefreshChatsApi, null, RefreshRate, RefreshRate);
            }
        }

        public async Task ForceUpdateChannel()
        {
            await RefreshChatsApi(false).ConfigureAwait(false);
        }

        private async void RefreshChatsApi(object sender)
        {
            try
            {
                Log.Debug("Running LivestreamChecker");
                await RefreshChatsApi(true).ConfigureAwait(false);
                Log.Debug("Finishing LivestreamChecker");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
            }
        }

        private async Task RefreshChatsApi(bool postMessage)
        {
            try
            {
                if (Program.Client.Guilds.Any())
                {
                    try
                    {
                        _latestPromo = await GetChatsFromUrl(ApiCallUrl, 0).ConfigureAwait(false);

                        var numberOfResults = _latestPromo.NumberOfPageResults;

                        var stream = _latestPromo.Results.FirstOrDefault(x => !IgnoreList.Contains(x.ChannelName));

                        if (_wasStreamRunning == false && numberOfResults != 0 && stream != null)
                        {
                            await Subscribe.PostLivestream(stream).ConfigureAwait(false);
                            await UpdateTask(stream, postMessage).ConfigureAwait(false);
                            _wasStreamRunning = true;
                        }
                        else if (_wasStreamRunning && (numberOfResults == 0 || stream == null))
                        {
                            await UpdateTask(stream, postMessage).ConfigureAwait(false);
                            _wasStreamRunning = false;
                        }

                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("LivestreamChecker timed out.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LivestreamChecker sucks: {ex} \n {ex.Message}");
                var ownerDmChannel = await Program.Client.GetDMChannelAsync(85817630560108544).ConfigureAwait(false);
                if (ownerDmChannel != null)
                    await ownerDmChannel.SendMessageAsync($"LivestreamChecker threw an {ex.GetType()}, check the logs").ConfigureAwait(false);
            }
        }

        private async Task UpdateTask(Result e, bool postMessage)
        {
            var isGbServer = Client.Guilds.Any(x => x.Id == 106386929506873344);
            if (e != null)
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)Client.GetChannel(channelId);

                    await channel.ModifyAsync(p =>
                    {
                        p.Name = "livestream-live";
                        p.Topic = $"Currently Live on Giant Bomb: {e.Title}\n http://www.giantbomb.com/chat/";
                    }).ConfigureAwait(false);

                    if (postMessage) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);

                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)Client.GetChannel(channelId);

                    if (postMessage) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);
                }
            }
            else
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)Client.GetChannel(channelId);
                    var nextLiveStream = (await UpcomingOn.DownloadUpcomingJson().ConfigureAwait(false)).Upcoming.FirstOrDefault(x => x.Type == "Live Show");

                    await channel.ModifyAsync(p =>
                    {
                        p.Name = "livestream";
                        p.Topic =
                            $"Chat for live broadcasts.\nUpcoming livestream: {(nextLiveStream != null ? nextLiveStream.Title + " on " + nextLiveStream.Date + " PST." + Environment.NewLine : "No upcoming livestream.")}";
                    }).ConfigureAwait(false);

                    if (postMessage)
                        await channel.SendMessageAsync(
                            "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                            await KiteChat.GetResponseUriFromRandomQlCrew()
                            .ConfigureAwait(false))
                            .ConfigureAwait(false);

                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)Client.GetChannel(channelId);

                    if (postMessage)
                        await channel.SendMessageAsync(
                            "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                            await KiteChat.GetResponseUriFromRandomQlCrew()
                            .ConfigureAwait(false))
                            .ConfigureAwait(false);
                }
            }
        }

        private async Task SendLivestreamMessageAsync(Result r, SocketTextChannel channel)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle($"{r.Title}  is LIVE NOW")
                .WithUrl(r.SiteDetailUrl)
                .WithDescription(r.Deck)
                .WithImageUrl(r.Image.ScreenUrl)
                .WithFooter(x => x.WithText("Giant Bomb"))
                .WithColor(new Color(0xFFEE00))
                .WithCurrentTimestamp();

            await channel.SendMessageAsync("", false, embedBuilder).ConfigureAwait(false);
        }

        private async Task<Chats> GetChatsFromUrl(string url, int retry)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot that calls api every {RefreshRate / 1000} seconds.");
                    Chats json = JsonConvert.DeserializeObject<Chats>(await client.GetStringAsync(url).ConfigureAwait(false));
                    return json;
                }
            }
            catch (Exception)
            {
                if (++retry < 3)
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    return await GetChatsFromUrl(url, retry).ConfigureAwait(false);
                }
                throw new TimeoutException();
            }
        }

        public void IgnoreChannel(string args)
        {
            IgnoreList.Add(args);
            File.WriteAllText(IgnoreFilePath, JsonConvert.SerializeObject(IgnoreList));
        }

        public async Task<string> ListChannelsAsync()
        {
            var result = await GetChatsFromUrl(ApiCallUrl, 0).ConfigureAwait(false);
            var streams = result.Results;
            var output = "";
            foreach (var stream in streams)
            {
                output += stream.ChannelName + Environment.NewLine;
            }
            return output;
        }
    }
}
