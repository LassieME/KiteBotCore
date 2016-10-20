using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class BotSettings
    {

        [JsonProperty("DiscordEmail")]
        public object DiscordEmail { get; set; }

        [JsonProperty("DiscordPassword")]
        public object DiscordPassword { get; set; }

        [JsonProperty("DiscordToken")]
        public string DiscordToken { get; set; }

        [JsonProperty("GiantBombApiKey")]
        public string GiantBombApiKey { get; set; }

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
