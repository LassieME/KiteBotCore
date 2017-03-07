using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class BotSettings
    {
        [JsonProperty("CommandPrefix")]
        public char CommandPrefix { get; set; }

        [JsonProperty("DiscordEmail")]
        public string DiscordEmail { get; set; }

        [JsonProperty("DiscordPassword")]
        public string DiscordPassword { get; set; }

        [JsonProperty("DiscordToken")]
        public string DiscordToken { get; set; }

        [JsonProperty("GiantBombApiKey")]
        public string GiantBombApiKey { get; set; }

        [JsonProperty("YoutubeApiKey")]
        public string YoutubeApiKey { get; set; }

        [JsonProperty("AnilistClientId")]
        public string AnilistId { get; set; }

        [JsonProperty("AnilistClientSecret")]
        public string AnilistSecret { get; set; }

        [JsonProperty("DatabaseConnectionString")]
        public string DatabaseConnectionString { get; set; }

        [JsonProperty("OwnerId")]
        public ulong OwnerId { get; set; }

        [JsonProperty("MarkovChainStart")]
        public bool MarkovChainStart { get; set; }

        [JsonProperty("MarkovChainDepth")]
        public int MarkovChainDepth { get; set; }

        [JsonProperty("GiantBombVideoRefreshRate")]
        public int GiantBombVideoRefreshRate { get; set; }

        [JsonProperty("GiantBombLiveStreamRefreshRate")]
        public int GiantBombLiveStreamRefreshRate { get; set; }
    }
}
