using Newtonsoft.Json;

namespace KiteBotCore.Json.GiantBomb.Chats
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

    internal class Result
    {

        [JsonProperty("api_detail_url")]
        public string ApiDetailUrl { get; set; }

        [JsonProperty("channel_name")]
        public object ChannelName { get; set; }

        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("image")]
        public Image Image { get; set; }

        [JsonProperty("password")]
        public object Password { get; set; }

        [JsonProperty("site_detail_url")]
        public string SiteDetailUrl { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    internal class Chats
    {

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("limit")]
        public object Limit { get; set; }

        [JsonProperty("offset")]
        public object Offset { get; set; }

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

}
