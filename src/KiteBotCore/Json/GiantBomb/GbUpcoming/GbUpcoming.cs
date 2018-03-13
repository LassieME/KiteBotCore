using System;
using Newtonsoft.Json;

namespace KiteBotCore.Json.GiantBomb.GbUpcoming
{
    public class GbUpcoming
    {

        [JsonProperty("liveNow")]
        public LiveNow LiveNow { get; set; }

        [JsonProperty("upcoming")]
        public Upcoming[] Upcoming { get; set; }
    }

    public class LiveNow
    {

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LiveNow now &&
                   Title == now.Title &&
                   Image == now.Image;
        }

        protected bool Equals(LiveNow other)
        {
            return string.Equals(Title, other.Title) && string.Equals(Image, other.Image);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Title != null ? Title.GetHashCode() : 0) * 397) ^ (Image != null ? Image.GetHashCode() : 0);
            }
        }

        public override string ToString() => Title != null ? $"Live NOW on Giantbomb.com: {Title} \r\n{Image}" : "No Livestream right now.";
    }

    public class Upcoming
    {

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Date { get; set; }

        [JsonProperty("premium")]
        public bool Premium { get; set; }

        public override string ToString() => 
            Premium ? 
            $"Upcoming Premium {Type} on {Date} PST: \n{Title}" : 
            $"Upcoming {Type} on {Date} PST: \n{Title}";
    }

}
