using Newtonsoft.Json;

namespace KiteBotCore.Json
{

    internal class MarkovMessage
    {
        [JsonProperty("M")]
        public string M { get; set; }
    }

}
