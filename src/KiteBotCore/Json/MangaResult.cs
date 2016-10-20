using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class MangaResult
    {
        [JsonProperty("id")]
        public int Id;
        [JsonProperty("publishing_status")]
        public string PublishingStatus;
        [JsonProperty("image_url_lge")]
        public string ImageUrlLge;
        [JsonProperty("title_english")]
        public string TitleEnglish;
        [JsonProperty("total_chapters")]
        public int TotalChapters;
        [JsonProperty("total_volumes")]
        public int TotalVolumes;
        [JsonProperty("description")]
        public string Description;

        public override string ToString() =>
            "`Title:` **" + TitleEnglish +
            "**\n`Status:` " + PublishingStatus +
            "\n`Chapters:` " + TotalChapters +
            "\n`Volumes:` " + TotalVolumes +
            "\n`Link:` http://anilist.co/manga/" + Id +
            "\n`Synopsis:` " + Description.Substring(0, Description.Length > 500 ? 500 : Description.Length) + "..." +
            "\n`img:` " + ImageUrlLge;
    }
}
