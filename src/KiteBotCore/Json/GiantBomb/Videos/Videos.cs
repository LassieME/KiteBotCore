using Newtonsoft.Json;

namespace KiteBotCore.Json.GiantBomb.Videos
{
    internal class Videos
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
        public Result[] Results { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

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

    internal class Result
    {
        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("hd_url")]
        public string HdUrl { get; set; }

        [JsonProperty("high_url")]
        public string HighUrl { get; set; }

        [JsonProperty("low_url")]
        public string LowUrl { get; set; }

        [JsonProperty("embed_player")]
        public string EmbedPlayer { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("length_seconds")]
        public int LengthSeconds { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("publish_date")]
        public string PublishDate { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("video_type")]
        public string VideoType { get; set; }

        [JsonProperty("youtube_id")]
        public string YoutubeId { get; set; }
    }
}
