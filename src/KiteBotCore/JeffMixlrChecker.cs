using System.Globalization;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using KiteBotCore.Modules;
using Serilog;

namespace KiteBotCore
{
    public class JeffMixlrChecker
    {
        public int RefreshRate;
        private readonly Timer _mixlrTimer; //Garbage collection doesnt like local timers.

        private readonly DiscordSocketClient _client;
        private bool _mixlrUserLive = false;

        public JeffMixlrChecker(DiscordSocketClient client, int streamRefresh, bool silentStartup)
        {
            _client = client;

            RefreshRate = streamRefresh;
            if (streamRefresh > 30000)
            {
                _mixlrTimer = new Timer(MixlrApi, null, 60000, RefreshRate);
            }

            if (silentStartup)
            {
                _mixlrUserLive = true;
            }
        }

        public async void MixlrApi(object sender)
        {
            try
            {
                Log.Verbose("Running JeffMixlrChecker");
                await RefreshChatsApi().ConfigureAwait(false);
                Log.Verbose("Finishing JeffMixlrChecker");
            }
            catch (Exception ex)
            {
                Console.WriteLine("LivestreamChecker exceptioned: " + ex.Message);
                Console.WriteLine(ex);
            }
        }

        private async Task RefreshChatsApi()
        {
            if (_client.Guilds.Count > 0)
            {
                var user = await RequestMixlrUser("jeff-gerstmann").ConfigureAwait(false);
                if (user.IsLive && !_mixlrUserLive)
                {
                    foreach (var clientGuild in _client.Guilds.Where(x => x.Id == 85814946004238336 || x.Id == 106386929506873344))
                    {
                        await UpdateTask(user, true, clientGuild.Id == 106386929506873344).ConfigureAwait(false);
                    }
                }

                _mixlrUserLive = user.IsLive;
            }
        }

        private async Task UpdateTask(MixlrUser e, bool postMessage, bool isGbServer)
        {
            if (e != null)
            {
                if (isGbServer)
                {
                    const ulong channelId = 106390533974294528;
                    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                    if (postMessage)
                    {
                        IMessage message = await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);
                        //await Subscribe.PostLivestream(message, _client).ConfigureAwait(false);
                    }
                }
                //else
                //{
                //    const ulong channelId = 85842104034541568;
                //    SocketTextChannel channel = (SocketTextChannel)_client.GetChannel(channelId);

                //    if (postMessage) await SendLivestreamMessageAsync(e, channel).ConfigureAwait(false);
                //}
            }
        }

        private async Task<IMessage> SendLivestreamMessageAsync(MixlrUser r, SocketTextChannel channel)
        {
            var embedBuilder = new EmbedBuilder();

            embedBuilder
                .WithTitle($"{r.Username} is LIVE on Mixlr")
                .WithDescription(r.AboutMe)
                .WithUrl(r.Url)
                .WithImageUrl(r.ProfileImageUrl)
                .WithColor(new Color(0xFFEE00))
                .WithCurrentTimestamp();

            return await channel.SendMessageAsync("", false, embedBuilder.Build()).ConfigureAwait(false);
        }

        private async Task<MixlrUser> RequestMixlrUser(string username)
        {
            string stringResult = null;
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    stringResult = await httpClient.GetStringAsync($"http://api.mixlr.com/users/{username}").ConfigureAwait(false);
                }
            }
            catch (ArgumentNullException ex)
            {
                Log.Error("MixlrChecker threw an " + ex + ex.Message);
            }
            catch (HttpRequestException ex)
            {
                Log.Error("MixlrChecker threw an " + ex + ex.Message);
            }
            return JsonConvert.DeserializeObject<MixlrUser>(stringResult);
        }
    }

    public class MixlrUser
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("profile_image_url")]
        public string ProfileImageUrl { get; set; }

        [JsonProperty("about_me")]
        public string AboutMe { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("permalink")]
        public string Permalink { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("is_live")]
        public bool IsLive { get; set; }

        [JsonProperty("broadcast_ids")]
        public string[] BroadcastIds { get; set; }

        [JsonProperty("time_zone")]
        public string TimeZone { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("is_premium")]
        public bool IsPremium { get; set; }

        [JsonProperty("plan")]
        public Plan Plan { get; set; }
    }

    public class Plan
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }
    }
}