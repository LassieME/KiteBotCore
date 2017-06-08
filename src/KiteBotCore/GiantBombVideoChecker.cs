using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using KiteBotCore.Json.GiantBomb.Promos;
using Newtonsoft.Json;
using Serilog;


namespace KiteBotCore
{
    public class GiantBombVideoChecker
	{
		public static string ApiCallUrl;
        public static int RefreshRate;
		private static Timer _chatTimer;//Garbage collection doesnt like local variables that only fire a couple times per hour.
        private DateTime _lastPublishTime;
        private bool _firstTime = true;
	    private readonly DiscordSocketClient _client;

        public GiantBombVideoChecker(DiscordSocketClient client, string GbAPI,int videoRefresh)
        {
            _client = client;
            if (GbAPI.Length > 0)
            {
                ApiCallUrl =
                    $"http://www.giantbomb.com/api/promos/?api_key={GbAPI}&field_list=name,deck,date_added,link,user&format=json";
                RefreshRate = videoRefresh;
                _chatTimer = new Timer(RefreshVideosApi, null, videoRefresh, videoRefresh);

            }
        }

        public void Restart()
        {
            if (_chatTimer == null)
            {
                Console.WriteLine("VideoChecker _chatTimer eaten by GC");
                Environment.Exit(1);
            }
        }

        private async void RefreshVideosApi(object sender)
		{
            try
            {
                Log.Verbose("Running Videochecker");
                await RefreshVideosApi();
                Log.Verbose("Finishing Videochecker");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex);
            }
		}

		private async Task RefreshVideosApi()
		{
            var latestPromo = await GetPromosFromUrl(ApiCallUrl,0);

            IOrderedEnumerable<Result> sortedXElements = latestPromo.Results.OrderBy(e => GetGiantBombFormatDateTime(e.DateAdded));

		    if (_firstTime)
		    {
		        _lastPublishTime = GetGiantBombFormatDateTime(sortedXElements.Last().DateAdded);
		        _firstTime = false;
		    }
		    else
		    {
		        foreach (Result item in sortedXElements)
		        {
                    DateTime newPublishTime = GetGiantBombFormatDateTime(item.DateAdded);
                    if (newPublishTime.CompareTo(_lastPublishTime) > 0)
                    {
                        var title = item.Name;
                        var deck = item.Deck;
                        var link = item.Link;
                        var user = item.User;
                        _lastPublishTime = newPublishTime;

                        ITextChannel channel = (ITextChannel) _client.GetChannel(85842104034541568);
                        await channel.SendMessageAsync(title + ": " + deck + Environment.NewLine + "by: " + user +
                                                       Environment.NewLine + link).ConfigureAwait(false);
                    }
                }
            }
		}
        private DateTime GetGiantBombFormatDateTime(string dateTimeString)
        {
            string timeString = dateTimeString;
            return DateTime.ParseExact(timeString,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture);
        }

        private async Task<Promos> GetPromosFromUrl(string url, int retry)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", $"KiteBotCore 1.1 GB Discord Bot that calls api every {RefreshRate / 1000} seconds.");
                    Promos json = JsonConvert.DeserializeObject<Promos>(await client.GetStringAsync(url).ConfigureAwait(false));
                    return json;
                }
            }
            catch (Exception)
            {
                if (++retry < 3)
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    return await GetPromosFromUrl(url,retry).ConfigureAwait(false);
                }
                throw new TimeoutException();
            }
        }
	}
}
