using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;

namespace KiteBotCore.Modules
{
    public class AnimeManga : ModuleBase
    {
        [Command("anime")]
        [Summary("Finds a anime from the anilist database")]
        public async Task AnimeCommand([Remainder] string animeTitle)
        {
            string search;
            try
            {
                search = (await SearchHelper.GetAnimeData(animeTitle))[0].ToString();
            }
            catch (JsonSerializationException)
            {
                search = "Can't find any anime with that name on anilist.";
            }
            await ReplyAsync(search);
        }

        [Command("manga")]
        [Summary("Finds a manga from the anilist database")]
        public async Task MangaCommand([Remainder] string mangaTitle)
        {
            string search;
            try
            {
                search = (await SearchHelper.GetMangaData(mangaTitle))[0].ToString();
            }
            catch (JsonSerializationException)
            {
                search = "Can't find any manga with that name on anilist.";
            }
            await ReplyAsync(search);
        }

        public static class SearchHelper
        {
            private static DateTime _lastRefreshed = DateTime.MinValue;
            private static string Token { get; set; } = "";

            public static async Task<Stream> GetResponseStreamAsync(string url,
                IEnumerable<KeyValuePair<string, string>> headers = null,
                RequestHttpMethod method = RequestHttpMethod.Get)
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentNullException(nameof(url));
                var httpClient = new HttpClient();
                switch (method)
                {
                    case RequestHttpMethod.Get:
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }
                        return await httpClient.GetStreamAsync(url);
                    case RequestHttpMethod.Post:
                        FormUrlEncodedContent formContent = null;
                        if (headers != null)
                        {
                            formContent = new FormUrlEncodedContent(headers);
                        }
                        var message = await httpClient.PostAsync(url, formContent);
                        return await message.Content.ReadAsStreamAsync();
                    default:
                        throw new NotImplementedException("That type of request is unsupported.");
                }
            }

            public static async Task<string> GetResponseStringAsync(string url,
                IEnumerable<KeyValuePair<string, string>> headers = null,
                RequestHttpMethod method = RequestHttpMethod.Get)
            {

                using (var streamReader = new StreamReader(await GetResponseStreamAsync(url, headers, method)))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }

            internal static async Task<List<AnimeSearchResult>> GetAnimeData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));

                await RefreshAnilistToken();

                var cl = new HttpClient();
                //cl.DefaultRequestHeaders.Add("access_token", Token);

                var rq = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query) + "?access_token=" + Token;
                var smallContent = await cl.GetStringAsync(rq);

                return JsonConvert.DeserializeObject<List<AnimeSearchResult>>(smallContent);

                //var smallObj = JArray.Parse(smallContent)[0];
                //rq = "http://anilist.co/anime/" + smallObj["id"];
                //var content = await cl.GetStringAsync(rq);
            }

            internal static async Task<List<MangaSearchResult>> GetMangaData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));

                await RefreshAnilistToken();

                var cl = new HttpClient();
                //cl.DefaultRequestHeaders.Add("access_token", Token);

                var rq = "http://anilist.co/api/manga/search/" + Uri.EscapeUriString(query) + "?access_token=" + Token;
                var smallContent = await cl.GetStringAsync(rq);

                return JsonConvert.DeserializeObject<List<MangaSearchResult>>(smallContent);

                //var smallObj = JArray.Parse(smallContent)[0];
                //rq = "http://anilist.co/manga/" + smallObj["id"];
                //var content = await cl.GetStringAsync(rq);
            }

            private static async Task RefreshAnilistToken()
            {
                if (DateTime.Now - _lastRefreshed > TimeSpan.FromMinutes(29))
                    _lastRefreshed = DateTime.Now;
                else
                {
                    return;
                }
                var headers = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", "kwoth-w0ki9"},
                    {"client_secret", "Qd6j4FIAi1ZK6Pc7N7V4Z"},
                };
                var content =
                    await
                        GetResponseStringAsync("http://anilist.co/api/auth/access_token", headers,
                            RequestHttpMethod.Post);

                Token = JObject.Parse(content)["access_token"].ToString();
            }

            public enum RequestHttpMethod
            {
                Get,
                Post
            }
        }
    }
}