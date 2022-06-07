using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json.GiantBomb.Chats;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using KiteBotCore.Modules;
using Newtonsoft.Json;
using Serilog;

namespace KiteBotCore
{
    public class LivestreamCheckerV2
    {
        public int RefreshRate;
        private readonly Timer _chatTimer; //Garbage collection doesnt like local timers.

        private readonly DiscordSocketClient _client;
        private readonly UpcomingJsonService _service;
        private LiveNow _liveNow;

        private readonly string _livestreamNotActiveName = "livestream";
        private readonly string _livestreamActiveName = "livestream-live";

        public LivestreamCheckerV2(DiscordSocketClient client, UpcomingJsonService service, int streamRefresh, bool silentStartup)
        {
            _client = client;
            _service = service;
            RefreshRate = streamRefresh;
            if (streamRefresh > 30000)
            {
                _chatTimer = new Timer(TimerEvent, null, 60000, RefreshRate);
            }

            _ = Task.Run(async () =>
            {
                await RunRefreshChatsApi(true, silentStartup).ConfigureAwait(false);
            });
        }

        private async void TimerEvent(object sender)
        {
            await RunRefreshChatsApi(false).ConfigureAwait(false);
        }

        private async Task RunRefreshChatsApi(bool isInitialRun, bool silentStartup = false)
        {
            try
            {
                Log.Verbose("Running LivestreamChecker");
                await RefreshChatsApi(isInitialRun, silentStartup).ConfigureAwait(false);
                Log.Verbose("Finishing LivestreamChecker");
            }
            catch (Exception ex)
            {
                Console.WriteLine("LivestreamChecker exceptioned: " + ex.Message);
                Console.WriteLine(ex);
            }
        }

        private async Task RefreshChatsApi(bool isInitialRun, bool silentStartup = false)
        {
            if (_client.Guilds.Count > 0)
            {
                var upcomingNew = await _service.DownloadUpcomingJsonAsync().ConfigureAwait(false);
                if (!Equals(upcomingNew.LiveNow, _liveNow))
                {
                    var isNewStream = _liveNow == null && upcomingNew.LiveNow != null;//Livestream has started
                    if (!silentStartup)
                    {
                        foreach (var clientGuild in _client.Guilds.Where(x => x.Id == 85814946004238336 || x.Id == 106386929506873344))
                        {
                            await UpdateTask(upcomingNew.LiveNow, postMessageToChannel: true, postMessageToSubscribers: isInitialRun ? false : isNewStream, isGbServer: clientGuild.Id == 106386929506873344)
                                .ConfigureAwait(false);
                        }
                    }
                    else //if silent startup is set, only update channel names and descriptions
                    {
                        foreach (var clientGuild in _client.Guilds.Where(x => x.Id == 85814946004238336 || x.Id == 106386929506873344))
                        {
                            await UpdateTask(upcomingNew.LiveNow, postMessageToChannel: false, postMessageToSubscribers: false, isGbServer: clientGuild.Id == 106386929506873344)
                                .ConfigureAwait(false);
                        }
                    }
                }
                _liveNow = upcomingNew.LiveNow;
            }
        }

        private async Task UpdateTask(LiveNow e, bool postMessageToChannel, bool postMessageToSubscribers, bool isGbServer)
        {
            if (e != null)
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    await channel.ModifyAsync(p =>
                    {
                        if(channel.Name != _livestreamActiveName)
                            p.Name = _livestreamActiveName;

                        p.Topic = $"Currently Live on Giant Bomb: {e.Title}\n http://www.giantbomb.com/chat/";
                    }).ConfigureAwait(false);

                    if (postMessageToChannel)
                    {
                        IMessage message = await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);

                        if(postMessageToSubscribers)
                            await Subscribe.PostLivestream(message, _client).ConfigureAwait(false);
                    }
                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    if (postMessageToChannel) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);
                }
            }
            else
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);
                    var nextLiveStream = Array.Find((await _service.DownloadUpcomingJsonAsync().ConfigureAwait(false)).Upcoming, x => x.Type == "Live Show");

                    await channel.ModifyAsync(p =>
                    {
                        p.Name = _livestreamNotActiveName;
                        p.Topic =
                            $"Chat for live broadcasts.\nUpcoming livestream: {(nextLiveStream != null ? nextLiveStream.Title + " on " + nextLiveStream.Date + " PST." + Environment.NewLine : "No upcoming livestream.")}";
                    }).ConfigureAwait(false);

                    if (postMessageToChannel)
                    {
                        await channel.SendMessageAsync(
                           "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                           await GetResponseUriFromRandomQlCrew().ConfigureAwait(true))
                           .ConfigureAwait(false);
                    }
                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    if (postMessageToChannel)
                    {
                        await channel.SendMessageAsync(
                           "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                           await GetResponseUriFromRandomQlCrew().ConfigureAwait(true))
                           .ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<IMessage> SendLivestreamMessageAsync(LiveNow r, SocketTextChannel channel)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle($"{r.Title}  is LIVE NOW")
                .WithUrl("https://www.giantbomb.com/chat/")
                .WithImageUrl(Uri.EscapeUriString(r.Image.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "" : "http://" + r.Image))
                .WithFooter(x => x.WithText("Giant Bomb"))
                .WithColor(new Color(0xFFEE00))
                .WithCurrentTimestamp();

            return await channel.SendMessageAsync("", false, embedBuilder.Build()).ConfigureAwait(false);
        }

        //TODO:Make this use the same method as misc:Randomql/RandomRyan
        public async Task<string> GetResponseUriFromRandomQlCrew()
        {
            const string url =
                "http://qlcrew.com/main.php?vid_type=ql&anyone=anyone&inc%5B0%5D=&exc%5B0%5D=&p=1&per_page=15&random";

            var request = (HttpWebRequest) WebRequest.Create(url);
            try
            {
                return (await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse)?.ResponseUri.AbsoluteUri;
            }
            catch (WebException ex)
            {
                var location = ex.Response.Headers.Get("Location");
                if (location != null)
                {
                    return location;
                }
                Console.WriteLine(ex + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + ex.Message);
            }

            return "Couldn't load QLcrew's Random Link.";
        }
    }
}