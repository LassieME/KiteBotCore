﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using Newtonsoft.Json;
using Discord;
using Serilog;

namespace KiteBotCore.Modules
{
    public class UpcomingOn : ModuleBase, IDisposable
    {
        public static string UpcomingUrl = "http://www.giantbomb.com/upcoming_json";
        public static Dictionary<string, string> ShorthandTz;
        private readonly HttpClient _client = new HttpClient();

        private Stopwatch _stopwatch;
        protected override void BeforeExecute(CommandInfo command)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        protected override void AfterExecute(CommandInfo command)
        {
            _stopwatch.Stop();
            Log.Debug($"Upcoming Command: {_stopwatch.ElapsedMilliseconds.ToString()} ms");
        }
        public UpcomingOn()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShorthandTz = new Dictionary<string, string>
                {
                    { "CET",  "Europe/Oslo" },
                    { "CEST", "Europe/Oslo" },
                    { "GMT",  "Europe/London" },
                    { "BST",  "Europe/London" },
                    { "EDT",  "America/New_York" },
                    { "EST",  "America/New_York" },
                    { "CST",  "America/Chicago" },
                    { "CDT",  "America/Chicago" },
                    { "MST",  "America/Denver" },
                    { "MDT",  "America/Denver" },
                    { "PDT",  "America/Los_Angeles" },
                    { "PST",  "America/Los_Angeles" },
                    { "JST",  "Asia/Tokyo" }
                };
            }
            else
            {
                ShorthandTz = new Dictionary<string, string>
                {
                    { "CET",  "Central Europe Standard Time" },
                    { "CEST", "Central Europe Standard Time" },
                    { "GMT",  "GMT Standard Time" },
                    { "BST",  "GMT Standard Time" },
                    { "EDT",  "Eastern Standard Time" },
                    { "EST",  "Eastern Standard Time" },
                    { "CST",  "Central Standard Time" },
                    { "CDT",  "Central Standard Time" },
                    { "MST",  "Mountain Standard Time" },
                    { "MDT",  "Mountain Standard Time" },
                    { "PDT",  "Pacific Standard Time" },
                    { "PST",  "Pacific Standard Time" },
                    { "JST",  "Tokyo Standard Time" }
                };
            }
        }

        [Command("upcoming")]
        [Alias("schedule")]
        [Summary("Lists upcoming content on Giant Bomb")]
        public async Task UpcomingNewCommand([Remainder] string inputTimeZone = null)
        {
            var json = await DownloadUpcomingJson().ConfigureAwait(false);
            string output = "";

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Upcoming on Giant Bomb")
                .WithDescription($"All dates are in {inputTimeZone ?? "PST"}")
                .WithColor(new Color(0x660066))
                .WithAuthor(x =>
                {
                    x.Name = "Giant Bomb";
                    x.Url = "http://giantbomb.com/";
                    x.IconUrl = "http://giantbomb.com/favicon.ico";
                })
                .WithThumbnailUrl("http://www.giantbomb.com/bundles/phoenixsite/images/core/loose/logo-gb-midsize.png");

            foreach (var upcoming in json.Upcoming)
            {
                embed.AddField(x =>
                {
                    x.Name = $"{(upcoming.Premium ? "Premium " + upcoming.Type : upcoming.Type)}: {upcoming.Title}";
                    x.Value = $@"on {( inputTimeZone != null ? TimeZoneInfo.ConvertTime(upcoming.Date,
                        TimeZoneInfo.FindSystemTimeZoneById(ShorthandTz["PST"]),
                        TimeZoneInfo.FindSystemTimeZoneById(ShorthandTz.TryGetValue(inputTimeZone.ToUpper(), out string input) ? input : inputTimeZone)) : upcoming.Date)}";
                });
            }

            await ReplyAsync(output, embed: embed).ConfigureAwait(false);
        }

        internal async Task<GbUpcoming> DownloadUpcomingJson()
        {
            _client.DefaultRequestHeaders.Add("User-Agent",
                "KiteBotCore 1.1 GB Discord Bot looking for upcoming content");
            GbUpcoming json = JsonConvert.DeserializeObject<GbUpcoming>(await _client.GetStringAsync(UpcomingUrl).ConfigureAwait(false));
            return json;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
