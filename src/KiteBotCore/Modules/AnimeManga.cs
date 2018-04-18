using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public IServiceProvider Services { get; set; }
        public SearchHelper SearchHelper { get; set; }
        public FollowUpService FollowUpService { get; set; }

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Anilist Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }

        [Command("anime")]
        [Summary("Finds a anime from the anilist database")]
        public async Task AnimeCommand([Remainder] string animeTitle)
        {
            string output = "";
            EmbedBuilder embed = null;
            try
            {
                var animedata = await SearchHelper.GetAnimeData(animeTitle).ConfigureAwait(false);
                if (animedata.Count == 1)
                {
                    embed = (await SearchHelper.GetAnimeData(animeTitle).ConfigureAwait(false))[0].ToEmbed();
                }
                else
                {
                    var dict = new Dictionary<string, Tuple<string, Func<EmbedBuilder>>>();

                    int i = 1;
                    output = "Which of these anime did you mean?" + Environment.NewLine;
                    foreach (var result in animedata)
                    {
                        if (i < 11 && result.TitleEnglish != null && result.TitleJapanese != null)
                        {
                            var name = result.TitleEnglish ?? result.TitleJapanese;
                            dict.Add(i.ToString(), Tuple.Create<string, Func<EmbedBuilder>>("", () => result.ToEmbed()));
                            output += $"{i++}. {name} {Environment.NewLine}";
                        }
                        else
                        {
                            break;
                        }
                    }
                    var messageToEdit = await ReplyAsync(output + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.").ConfigureAwait(false);
                    FollowUpService.AddNewFollowUp(new FollowUp(Services, dict, Context.User.Id, Context.Channel.Id, messageToEdit));
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
            await ReplyAsync(output, false, embed.Build()).ConfigureAwait(false);
        }

        [Command("manga")]
        [Summary("Finds a manga from the anilist database")]
        public async Task MangaCommand([Remainder] string mangaTitle)
        {
            string output = "";
            EmbedBuilder embed = null;
            try
            {
                var mangaData = await SearchHelper.GetMangaData(mangaTitle).ConfigureAwait(false);
                if (mangaData.Count == 1)
                {
                    embed = (await SearchHelper.GetAnimeData(mangaTitle).ConfigureAwait(false))[0].ToEmbed();
                }
                else
                {
                    var dict = new Dictionary<string, Tuple<string, Func<EmbedBuilder>>>();

                    int i = 1;
                    output = "Which of these manga did you mean?" + Environment.NewLine;
                    foreach (var result in mangaData)
                    {
                        if (i < 11 && result.TitleEnglish != null && result.TitleJapanese != null)
                        {
                            var name = result.TitleEnglish ?? result.TitleRomaji ?? result.TitleJapanese;
                            try
                            {
                                dict.Add(i.ToString(), Tuple.Create<string, Func<EmbedBuilder>>("", () => result.ToEmbed()));
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
                    var messageToEdit = await ReplyAsync(output + "Just type the number you want, this command will self-destruct in 2 minutes if no action is taken.").ConfigureAwait(false);
                    FollowUpService.AddNewFollowUp(new FollowUp(Services, dict, Context.User.Id, Context.Channel.Id, messageToEdit));
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
            await ReplyAsync(output, false, embed.Build()).ConfigureAwait(false);
        }
    }
    public class SearchHelper
    {
        private DateTime _lastRefreshed = DateTime.MinValue;
        private string Token { get; set; } = "";

        private readonly Dictionary<string, string> _headers;

        internal SearchHelper(string clientId, string clientSecret)
        {
            _headers = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
                {"client_id", clientId},
                {"client_secret", clientSecret}
            };
        }

        internal async Task<Stream> GetResponseStreamAsync(string url, IEnumerable<KeyValuePair<string, string>> headers = null, RequestHttpMethod method = RequestHttpMethod.Get)
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
                    return await httpClient.GetStreamAsync(url).ConfigureAwait(false);
                case RequestHttpMethod.Post:
                    FormUrlEncodedContent formContent = null;
                    if (headers != null)
                    {
                        formContent = new FormUrlEncodedContent(headers);
                    }
                    var message = await httpClient.PostAsync(url, formContent).ConfigureAwait(false);
                    return await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
                default:
                    throw new NotImplementedException("That type of request is unsupported.");
            }
        }

        internal async Task<string> GetResponseStringAsync(string url,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            RequestHttpMethod method = RequestHttpMethod.Get)
        {

            using (var streamReader = new StreamReader(await GetResponseStreamAsync(url, headers, method).ConfigureAwait(false)))
            {
                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        internal async Task<List<AnimeSearchResult>> GetAnimeData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            await RefreshAnilistToken().ConfigureAwait(false);

            using (var cl = new HttpClient())
            {
                var rq = "https://anilist.co/api/anime/search/" + Uri.EscapeUriString(query) + "?access_token=" +
                         Token;
                var smallContent = await cl.GetStringAsync(rq).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<List<AnimeSearchResult>>(smallContent);
            }
        }

        internal async Task<List<MangaSearchResult>> GetMangaData(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            await RefreshAnilistToken().ConfigureAwait(false);

            using (var cl = new HttpClient())
            {
                var rq = "https://anilist.co/api/manga/search/" + Uri.EscapeUriString(query) + "?access_token=" + Token;
                var smallContent = await cl.GetStringAsync(rq).ConfigureAwait(false);

                return JsonConvert.DeserializeObject<List<MangaSearchResult>>(smallContent);
            }
        }

        private async Task RefreshAnilistToken()
        {
            if (DateTime.Now - _lastRefreshed > TimeSpan.FromMinutes(29))
                _lastRefreshed = DateTime.Now;
            else
            {
                return;
            }
            var headers = _headers;
            var content = await GetResponseStringAsync("https://anilist.co/api/auth/access_token", headers,
                RequestHttpMethod.Post).ConfigureAwait(false);

            Token = JObject.Parse(content)["access_token"].ToString();
        }

        internal enum RequestHttpMethod
        {
            Get,
            Post
        }
    }
}