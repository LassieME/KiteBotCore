using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KiteBotCore.Modules.Rank
{
    public class RankConfigs
    {
        [JsonProperty("GuildConfigs")]
        public Dictionary<ulong, GuildRanks> GuildConfigs { get; set; }

        public class GuildRanks
        {
            [JsonProperty("GuildId")]
            public ulong GuildId { get; set; }

            [JsonProperty("Roles")]
            public Dictionary<ulong, Rank> Ranks { get; set; }

            public class Rank
            {
                [JsonProperty("RoleId")]
                public ulong RoleId { get; set; }

                [JsonProperty("RoleTimeSpan")]
                public TimeSpan RequiredTimeSpan { get; set; }

                [JsonProperty("RoleColors")]
                public List<ulong> Colors { get; set; }
            }
        }
    }
}