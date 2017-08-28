using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KiteBotCore.Json.GiantBomb
{
    public class GBAppResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("creationTime")]
        public string CreationTime { get; set; }

        [JsonProperty("regToken")]
        public string RegToken { get; set; }

        [JsonProperty("customerId")]
        public object CustomerId { get; set; }
    }
}