using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json;
using Discord;
using Serilog;

namespace KiteBotCore.Modules
{
    public class AnimeManga : ModuleBase
    {
        private readonly IDependencyMap _map;

        public AnimeManga(IDependencyMap map)
        {
            _map = map;
        }

        [Command("anime")]
        [Summary("Finds a anime from the anilist database")]
        public async Task AnimeCommand([Remainder] string animeTitle)
        {
            string output = "";
            EmbedBuilder embed = null;
            try
            {
                var animedata = await SearchHelper.GetAnimeData(animeTitle);
                if (animedata.Count == 1)
                {
                    embed = (await SearchHelper.GetAnimeData(animeTitle))[0].ToEmbed();
                }
                else
                {
                    var dict = new Dictionary<string, Tuple<string, EmbedBuilder>>();

                    int i = 1;
                    output = "Which of these anime did you mean?" + Environment.NewLine;
                    foreach (var result in animedata)
                    {
                        if (i < 11 && result.TitleEnglish != null && result.TitleJapanese != null)
                        {
                            var name = result.TitleEnglish ?? result.TitleJapanese;
                            dict.Add(i.ToString(), Tuple.Create("", result.ToEmbed()));//TODO: Change to embed
                            output += $"{i++}. {name} {Environment.NewLine}";
                        }
                        else
                        {
                            break;
                        }
                    }
                    var messageToEdit = await ReplyAsync(output + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.");
                    FollowUpService.AddNewFollowUp(new FollowUp(_map, dict, Context.User.Id, Context.Channel.Id, messageToEdit));
                    return;
                }
            }
            catch (JsonSerializationException ex)
            {
                output = "Can't find any anime with that name on anilist.";
                Log.Verbose(ex + ex.Message);
            }
            catch (Exception ex)
            {
                output = "Some other error happened, check the logs.";
                Log.Debug(ex + ex.Message);
            }
            await ReplyAsync(output, false, embed);
        }

        [Command("manga")]
        [Summary("Finds a manga from the anilist database")]
        public async Task MangaCommand([Remainder] string mangaTitle)
        {
            string output = "";
            EmbedBuilder embed = null;
            try
            {
                var mangaData = await SearchHelper.GetMangaData(mangaTitle);
                if (mangaData.Count == 1)
                {
                    embed = (await SearchHelper.GetAnimeData(mangaTitle))[0].ToEmbed();
                }
                else
                {
                    var dict = new Dictionary<string, Tuple<string, EmbedBuilder>>();

                    int i = 1;
                    output = "Which of these manga did you mean?" + Environment.NewLine;
                    foreach (var result in mangaData)
                    {
                        if (i < 11 && result.TitleEnglish != null && result.TitleJapanese != null)
                        {
                            var name = result.TitleEnglish ?? result.TitleRomaji ?? result.TitleJapanese;
                            try
                            {
                                dict.Add(i.ToString(), Tuple.Create("", result.ToEmbed()));//TODO: Change to embed);
                            }
                            catch (Exception ex)
                            {
                                Log.Information($"{ex} - \n {ex.Message}");
                            }
                            output += $"{i++}. {name} {Environment.NewLine}";
                        }
                        else
                        {
                            break;
                        }
                    }
                    var messageToEdit = await ReplyAsync(output + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.");
                    FollowUpService.AddNewFollowUp(new FollowUp(_map, dict, Context.User.Id, Context.Channel.Id, messageToEdit));
                    return;
                }
            }
            catch (JsonSerializationException ex)
            {
                output = "Can't find any manga with that name on anilist.";
                Log.Verbose(ex + ex.Message);
            }
            catch (Exception ex)
            {
                output = "Some other error happened, check the logs.";
                Log.Debug(ex + ex.Message);
            }
            await ReplyAsync(output, false, embed);
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

                using (var cl = new HttpClient())
                {
                    var rq = "http://anilist.co/api/anime/search/" + Uri.EscapeUriString(query) + "?access_token=" +
                             Token;
                    var smallContent = await cl.GetStringAsync(rq);

                    return JsonConvert.DeserializeObject<List<AnimeSearchResult>>(smallContent);
                }
            }

            internal static async Task<List<MangaSearchResult>> GetMangaData(string query)
            {
                if (string.IsNullOrWhiteSpace(query))
                    throw new ArgumentNullException(nameof(query));

                await RefreshAnilistToken();

                using (var cl = new HttpClient())
                {
                    var rq = "http://anilist.co/api/manga/search/" + Uri.EscapeUriString(query) + "?access_token=" + Token;
                    var smallContent = await cl.GetStringAsync(rq);

                    return JsonConvert.DeserializeObject<List<MangaSearchResult>>(smallContent);
                }
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