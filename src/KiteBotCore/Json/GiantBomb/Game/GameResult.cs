using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KiteBotCore.Json.GiantBomb.GameResult
{

    internal class Image
    {
        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("medium_url")]
        public string MediumUrl { get; set; }

        [JsonProperty("screen_url")]
        public string ScreenUrl { get; set; }

        [JsonProperty("small_url")]
        public string SmallUrl { get; set; }

        [JsonProperty("super_url")]
        public string SuperUrl { get; set; }

        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty("tiny_url")]
        public string TinyUrl { get; set; }
    }

    internal class Platform
    {
        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }
    }

    internal class Results
    {
        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("expected_release_day")]
        public int? ExpectedReleaseDay { get; set; }

        [JsonProperty("expected_release_month")]
        public int? ExpectedReleaseMonth { get; set; }

        [JsonProperty("expected_release_quarter")]
        public int? ExpectedReleaseQuarter { get; set; }

        [JsonProperty("expected_release_year")]
        public int? ExpectedReleaseYear { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("original_release_date")]
        public string OriginalReleaseDate { get; set; }

        [JsonProperty("platforms")]
        public Platform[] Platforms { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        public EmbedBuilder ToEmbed()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(Name)
                .WithUrl(SiteDetailUrl)
                .WithDescription(Deck ?? "No Deck")
                .WithImageUrl(Image?.MediumUrl ?? Image?.SmallUrl ?? Image?.ThumbUrl ?? Image?.SuperUrl ?? "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/200px-No_image_available.svg.png")
                .WithFooter(x => x.Text = "Giant Bomb")
                .WithColor(new Discord.Color(0x00CC00))
                .WithCurrentTimestamp();

            if (OriginalReleaseDate != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "First release date";
                    x.Value = OriginalReleaseDate?.Replace(" 00:00:00", "");
                    x.IsInline = true;
                });
            }
            else if (ExpectedReleaseDay != null && ExpectedReleaseMonth != null && ExpectedReleaseYear != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "Expected release date";
                    x.Value =
                        $"{ExpectedReleaseYear}-{(ExpectedReleaseMonth < 10 ? "0" + ExpectedReleaseMonth : ExpectedReleaseMonth.ToString())}-{ExpectedReleaseDay}";
                    x.IsInline = true;
                });
            }
            else if (ExpectedReleaseQuarter != null && ExpectedReleaseYear != null)
            {
                embedBuilder.AddField(x =>
                {
                    x.Name = "Expected release quarter";
                    x.Value = $"Q{ExpectedReleaseQuarter} {ExpectedReleaseYear}";
                    x.IsInline = true;
                });
            }

            if (Platforms != null && Platforms.Any())
                embedBuilder.AddField(x =>
                {
                    x.Name = "Platforms";
                    x.Value = Platforms != null ? string.Join(", ", Platforms?.Select(y => y.Name)) : null;
                    x.IsInline = true;
                });

            return embedBuilder;
        }
    }

    internal class GameResult
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonProperty("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("results")]
        public Results Results { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

}
