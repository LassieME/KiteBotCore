using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ExtendedGiantBombClient.Interfaces;
using GiantBomb.Api;
using KiteBotCore;
using KiteBotCore.Utils;
using RestSharp.Portable;
using Serilog;

namespace ExtendedGiantBombClient
{
    public partial class ExtendedGiantBombRestClient : GiantBombRestClient, IExtendedGiantBombRestClient, IDisposable
    {
        internal static ConcurrentDictionary<string, TimeSpanSemaphore> RatelimitDictionary = new ConcurrentDictionary<string, TimeSpanSemaphore>();

        public ExtendedGiantBombRestClient(string apiToken, Uri baseUrl) : base(apiToken, baseUrl)
        {
        }

        public ExtendedGiantBombRestClient(string apiToken) : base(apiToken)
        {
        }

        /// <inheritdoc cref="GiantBombRestClient.ExecuteAsync" />
        public override Task<IRestResponse> ExecuteAsync(RestRequest request)
        {
            var route = request.Resource;
            if (!RatelimitDictionary.TryGetValue(route, out var semaphore))
            {
                Log.Information($"Couldn't find semaphore in dict, creating a new one for route \"{route}\"");
                semaphore = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(1.1));
                RatelimitDictionary.TryAdd(route, semaphore);
            }
            return semaphore.RunAsync(async () => await base.ExecuteAsync(request).ConfigureAwait(false));
        }

        /// <inheritdoc cref="GiantBombRestClient.ExecuteAsync{T}" />
        public override Task<T> ExecuteAsync<T>(RestRequest request)
        {
            var route = request.Resource;
            if (!RatelimitDictionary.TryGetValue(route, out var semaphore))
            {
                Log.Information($"Couldn't find semaphore in dict, creating a new one for route \"{route}\"");
                semaphore = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(1.1));
                RatelimitDictionary.TryAdd(route, semaphore);
            }
            return semaphore.RunAsync(async () => await base.ExecuteAsync<T>(request).ConfigureAwait(false));
        }

        public void Dispose()
        {
        }
    }
}