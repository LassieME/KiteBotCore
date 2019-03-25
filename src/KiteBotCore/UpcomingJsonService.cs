using KiteBotCore.Json.GiantBomb.GbUpcoming;
using KiteBotCore.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace KiteBotCore
{
    public class UpcomingJsonService
    {
        private readonly string UpcomingUrl = "http://www.giantbomb.com/upcoming_json";
        private readonly HttpClient _client = new HttpClient();
        private readonly TimeSpanSemaphore _rateLimit = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(2));

        public UpcomingJsonService()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "KiteBotCore 1.1 GB Discord Bot");
        }

        public Task<GbUpcoming> DownloadUpcomingJsonAsync()
        {
            try
            {
                return _rateLimit.RunAsync(async () =>
                    JsonConvert.DeserializeObject<GbUpcoming>(await _client.GetStringAsync(UpcomingUrl)
                        .ConfigureAwait(false)));
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case OperationCanceledException operationCanceledException:
                        Log.Error(ex,"HttpClient request timed out");
                        break;
                    case HttpRequestException httpRequestException:
                        Log.Error(ex, "HttpRequestException");
                        break;
                    default:
                        Log.Error(ex, "Some other unexpected error happened");
                        break;
                }

                throw;
            }
        }
    }
}