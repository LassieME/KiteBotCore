using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord;

namespace KiteBotCore.Json
{
    internal class AnimeListStats
    {
        [JsonProperty("completed")]
        public int Completed { get; set; }

        [JsonProperty("on_hold")]
        public int OnHold { get; set; }

        [JsonProperty("dropped")]
        public int Dropped { get; set; }

        [JsonProperty("plan_to_watch")]
        public int PlanToWatch { get; set; }

        [JsonProperty("watching")]
        public int Watching { get; set; }
    }

    internal class AnimeSearchResult
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title_romaji")]
        public string TitleRomaji { get; set; }

        [JsonProperty("title_english")]
        public string TitleEnglish { get; set; }

        [JsonProperty("title_japanese")]
        public string TitleJapanese { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("series_type")]
        public string SeriesType { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        [JsonProperty("end_date")]
        public string EndDate { get; set; }

        [JsonProperty("start_date_fuzzy")]
        public int? StartDateFuzzy { get; set; }

        [JsonProperty("end_date_fuzzy")]
        public int? EndDateFuzzy { get; set; }

        [JsonProperty("season")]
        public object Season { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("adult")]
        public bool Adult { get; set; }

        [JsonProperty("average_score")]
        public double AverageScore { get; set; }

        [JsonProperty("popularity")]
        public int? Popularity { get; set; }

        [JsonProperty("favourite")]
        public bool Favourite { get; set; }

        [JsonProperty("image_url_sml")]
        public string ImageUrlSml { get; set; }

        [JsonProperty("image_url_med")]
        public string ImageUrlMed { get; set; }

        [JsonProperty("image_url_lge")]
        public string ImageUrlLge { get; set; }

        [JsonProperty("image_url_banner")]
        public string ImageUrlBanner { get; set; }

        [JsonProperty("genres")]
        public string[] Genres { get; set; }

        [JsonProperty("synonyms")]
        public string[] Synonyms { get; set; }

        [JsonProperty("youtube_id")]
        public object YoutubeId { get; set; }

        [JsonProperty("hashtag")]
        public object Hashtag { get; set; }

        [JsonProperty("updated_at")]
        public int UpdatedAt { get; set; }

        [JsonProperty("score_distribution")]
        public object ScoreDistribution { get; set; }

        [JsonProperty("list_stats")]
        public AnimeListStats ListStats { get; set; }

        [JsonProperty("total_episodes")]
        public int? TotalEpisodes { get; set; }

        [JsonProperty("duration")]
        public int? Duration { get; set; }

        [JsonProperty("airing_status")]
        public string AiringStatus { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        //[JsonProperty("airing_stats")]
        //public object[] AiringStats { get; set; }

        public override string ToString() =>
        "`Title:` **" + TitleEnglish +
        "**\n`Status:` " + AiringStatus +
        "\n`Episodes:` " + TotalEpisodes +
        "\n`Link:` http://anilist.co/anime/" + Id +
        "\n`Synopsis:` " + Description.Substring(0, Description.Length > 500 ? 500 : Description.Length) + "..." +
        "\n`img:` " + ImageUrlLge;

        public EmbedBuilder ToEmbed()
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            
            embedBuilder
                .WithTitle($"{TitleEnglish ?? ""} ({TitleRomaji ?? TitleJapanese})")
                .WithUrl("http://anilist.co/anime/" + Id)
                .WithDescription(Description?.Substring(0, Description.Length > 500 ? 500 : Description.Length) + "...")
                .AddField(x =>
                {
                    x.Name = "Airing status";
                    x.Value = AiringStatus ?? "Unknown";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Episodes";
                    x.Value = TotalEpisodes.ToString() ?? "Unknown";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Genres";
                    x.Value = string.Join(", ", Genres);
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Start and End dates";
                    x.Value = $"{StartDate?.Replace("T00:00:00+09:00", "") ?? "Unknown"} - {EndDate?.Replace("T00:00:00+09:00", "") ?? "Unknown"}";
                    x.IsInline = true;
                })
                .AddField(x =>
                {
                    x.Name = "Adult Content";
                    x.Value = Adult ? "Yes" : "No";
                    x.IsInline = true;
                })
                .WithFooter(x => x.Text = "anilist.co")
                .WithColor(new Color(0x00CC00))
                .WithImageUrl(ImageUrlLge ?? null)
                .WithCurrentTimestamp();            
            return embedBuilder;
        }
    }
}
