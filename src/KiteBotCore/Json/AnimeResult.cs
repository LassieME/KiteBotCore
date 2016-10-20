using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class AnimeResult
    {
        [JsonProperty("id")]
        public int Id;
        [JsonProperty("airing_status")]
        public string AiringStatus;
        [JsonProperty("title_english")]
        public string TitleEnglish;
        [JsonProperty("total_episodes")]
        public int TotalEpisodes;
        [JsonProperty("description")]
        public string Description;
        [JsonProperty("image_url_lge")]
        public string ImageUrlLge;

        public override string ToString() =>
            "`Title:` **" + TitleEnglish +
            "**\n`Status:` " + AiringStatus +
            "\n`Episodes:` " + TotalEpisodes +
            "\n`Link:` http://anilist.co/anime/" + Id +
            "\n`Synopsis:` " + Description.Substring(0, Description.Length > 500 ? 500 : Description.Length) + "..." +
            "\n`img:` " + ImageUrlLge;
    }
}
