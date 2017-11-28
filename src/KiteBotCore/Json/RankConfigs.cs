using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class RankConfigs
    {
        [JsonProperty("GuildConfigs")]
        public Dictionary<ulong, GuildRanks> GuildConfigs { get; set; }
    }

    public class GuildRanks
    {
        [JsonProperty("GuildId")]
        public ulong GuildId { get; set; }

        [JsonProperty("Roles")]
        public Dictionary<ulong, Rank> Ranks { get; set; }

        [JsonProperty("Premium")]
        public Premium Premium { get; set; }
    }

    public class Premium : IRankRole
    {
        [JsonProperty("Id")]
        public ulong Id { get; set; }

        public TimeSpan RequiredTimeSpan {
            get => TimeSpan.Zero;
            set{ ; }
        }

        [JsonProperty("Colors")]
        public List<Color> Colors { get; set; }
    }

    public class Rank : IRankRole
    {
        [JsonProperty("RoleId")]
        public ulong Id { get; set; }

        [JsonProperty("RoleTimeSpan")]
        public TimeSpan RequiredTimeSpan { get; set; }

        [JsonProperty("RoleColors")]
        public List<Color> Colors { get; set; }
    }

    public class Color : IColor
    {
        [JsonProperty("ColorId")]
        public ulong Id { get; set; }
    }
}