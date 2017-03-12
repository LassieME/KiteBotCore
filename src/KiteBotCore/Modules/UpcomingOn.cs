﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.Commands;
using KiteBotCore.Json.GiantBomb.GbUpcoming;
using Newtonsoft.Json;
using Discord;

namespace KiteBotCore.Modules
{
    public class UpcomingOn : ModuleBase
    {
        public static string UpcomingUrl = "http://www.giantbomb.com/upcoming_json";
        public static Dictionary<string, string> ShorthandTz;

        public UpcomingOn()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShorthandTz = new Dictionary<string, string>
                {
                    { "CET",  "Europe/Oslo" },
                    { "CEST", "Europe/Oslo" },
                    { "GMT",  "Europe/London" },
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
                    { "EDT",  "Eastern Standard Time" },
                    { "EST",  "Eastern Standard Time" },
                    { "CST",  "Central Standard Time" },
                    { "CDT",  "Central Standard Time" },
                    { "MST",  "Mountain Standard Time" },
                    { "MDT",  "Mountain Standard Time" },
                    { "PDT",  "Pacific Standard Time" },
                    { "PST",  "Pacific Standard Time" },
                    { "TST",  "Tokyo Standard Time" },
                    { "JST",  "Tokyo Standard Time" }
                };
            }
        }

        [Command("upcoming")]
        [Summary("Lists upcoming content on Giant Bomb")]
        public async Task UpcomingNewCommand([Remainder] string timeZone = null)
        {
            var json = await TestDownload();
            string output = "";
            
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Upcoming on Giant Bomb")
                .WithDescription($"All dates are in {timeZone ?? "PST"}")
                .WithColor(new Color(0x660066))
                .WithAuthor(x =>
                {
                    x.Name = "Giant Bomb";
                    x.Url = "http://giantbomb.com/";
                    x.IconUrl = "http://giantbomb.com/favicon.ico";
                })
                .WithThumbnailUrl("http://www.giantbomb.com/bundles/phoenixsite/images/core/loose/logo-gb-midsize.png");

            if (timeZone == null)
            {
                foreach (var upcoming in json.Upcoming)
                {
                    embed.AddField(x =>
                    {
                        x.Name = $"{(upcoming.Premium ? "Premium " + upcoming.Type : upcoming.Type)}: {upcoming.Title}";
                        x.Value = $"on {upcoming.Date}";
                    });
                }
            }
            else
            {
                TimeZoneInfo pstZoneInfo =
                    TimeZoneInfo.FindSystemTimeZoneById(ShorthandTz["PST"]);
                string input;
                TimeZoneInfo timeZoneInfo =
                    TimeZoneInfo.FindSystemTimeZoneById(ShorthandTz.TryGetValue(timeZone, out input) ? input : timeZone);

                foreach (var upcoming in json.Upcoming)
                {
                    embed.AddField(x =>
                    {
                        x.Name = $"{(upcoming.Premium ? "Premium " + upcoming.Type : upcoming.Type)}: {upcoming.Title}";
                        x.Value = $"on {TimeZoneInfo.ConvertTime(upcoming.Date, pstZoneInfo, timeZoneInfo)}";
                    });
                }
            }
            await ReplyAsync(output, embed: embed);
        }

        internal static async Task<GbUpcoming> TestDownload()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "KiteBotCore 1.1 GB Discord Bot looking for upcoming content");
                GbUpcoming json = JsonConvert.DeserializeObject<GbUpcoming>(await client.GetStringAsync(UpcomingUrl));
                return json;
            }
        }
    }
}
