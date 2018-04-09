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
        private Timer _chatTimer; //Garbage collection doesnt like local timers.

        private readonly DiscordSocketClient _client;
        private readonly UpcomingJsonService _service;
        private LiveNow _liveNow = null;

        private string _livestreamNotActiveName = "livestream";
        private string _livestreamActiveName = "livestream-live";
        
        public LivestreamCheckerV2(DiscordSocketClient client, UpcomingJsonService service, int streamRefresh, bool silentStartup)
        {
            _client = client;
            _service = service;
            RefreshRate = streamRefresh;
            if (streamRefresh > 30000){
                
                _chatTimer = new Timer(RefreshChatsApi, null, 60000, RefreshRate);
            }
            if (silentStartup)
            {
                
                _liveNow = new LiveNow();
            }
        }

        private async void RefreshChatsApi(object sender)
        {
            try
            {
                Log.Verbose("Running LivestreamChecker");
                await RefreshChatsApi(true).ConfigureAwait(false);
                Log.Verbose("Finishing LivestreamChecker");
            }
            catch (Exception ex)
            {
                Console.WriteLine("LivestreamChecker exceptioned: " + ex.Message);
                Console.WriteLine(ex);
            }
        }

        private async Task RefreshChatsApi(bool postMessage)
        {
            if (_client.Guilds.Any())
            {
                var upcomingNew = await _service.DownloadUpcomingJsonAsync();
                if (!Equals(upcomingNew.LiveNow, _liveNow))
                {
                    if (_liveNow == null ^ upcomingNew.LiveNow == null)//Livestream has started or ended
                    {
                        foreach (var clientGuild in _client.Guilds.Where(x => x.Id == 85814946004238336 || x.Id == 106386929506873344))
                        {
                            await UpdateTask(upcomingNew.LiveNow, true, clientGuild.Id == 106386929506873344);
                        }
                    }
                    else
                    {
                        Log.Information("Livestream object has changed but not gone on/offline");
                    }
                }
                _liveNow = upcomingNew.LiveNow;
            }
        }
        private async Task UpdateTask(LiveNow e, bool postMessage, bool isGbServer)
        {
            if (e != null)
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    await channel.ModifyAsync(p =>
                    {
                        p.Name = _livestreamActiveName;
                        p.Topic = $"Currently Live on Giant Bomb: {e.Title}\n http://www.giantbomb.com/chat/";
                    }).ConfigureAwait(false);

                    if (postMessage) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);

                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    if (postMessage) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);
                }
            }
            else
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);
                    var nextLiveStream = (await _service.DownloadUpcomingJsonAsync().ConfigureAwait(false)).Upcoming.FirstOrDefault(x => x.Type == "Live Show");

                    await channel.ModifyAsync(p =>
                    {
                        p.Name = _livestreamNotActiveName;
                        p.Topic =
                            $"Chat for live broadcasts.\nUpcoming livestream: {(nextLiveStream != null ? nextLiveStream.Title + " on " + nextLiveStream.Date + " PST." + Environment.NewLine : "No upcoming livestream.")}";
                    }).ConfigureAwait(false);

                    if (postMessage)
                        await channel.SendMessageAsync(
                            "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                            await GetResponseUriFromRandomQlCrew()
                            .ConfigureAwait(false))
                            .ConfigureAwait(false);

                }
                else
                {
                    const ulong channelId = 85842104034541568;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    if (postMessage)
                        await channel.SendMessageAsync(
                            "Show is over folks, if you need more Giant Bomb videos, check this out: " +
                            await GetResponseUriFromRandomQlCrew()
                            .ConfigureAwait(false))
                            .ConfigureAwait(false);
                }
            }
        }
        private async Task SendLivestreamMessageAsync(LiveNow r, SocketTextChannel channel)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle($"{r.Title}  is LIVE NOW")
                .WithUrl("https://www.giantbomb.com/chat/")
                .WithImageUrl("http://" + r.Image)
                .WithFooter(x => x.WithText("Giant Bomb"))
                .WithColor(new Color(0xFFEE00))
                .WithCurrentTimestamp();

            await channel.SendMessageAsync("", false, embedBuilder).ConfigureAwait(false);
        }

        public async Task<string> GetResponseUriFromRandomQlCrew()
        {
            string url =
                "http://qlcrew.com/main.php?vid_type=ql&anyone=anyone&inc%5B0%5D=&exc%5B0%5D=&p=1&per_page=15&random";


            var request = (HttpWebRequest) WebRequest.Create(url);
            if (request != null)
            {
                HttpWebResponse response = await request.GetResponseAsync().ConfigureAwait(false) as HttpWebResponse;
                return response?.ResponseUri.AbsoluteUri;
            }

            return "Couldn't load QLcrew's Random Link.";
        }
    }
}