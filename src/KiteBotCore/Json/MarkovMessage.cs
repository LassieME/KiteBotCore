using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace KiteBotCore.Json
{
    public class MarkovMessage
    {
        //public long Id { get; set; }

        [JsonProperty("M")]
        public string M { get; set; }

        // Avoid modifying the following directly.
        // Used as a database column only.
        public long MarkovMessageId { get; set; }

        // Access/modify this variable instead.
        // Tell EF not to map this field to a Db table
        [NotMapped]
        public ulong Id
        {
            get
            {
                unchecked
                {
                    return (ulong) MarkovMessageId;
                }
            }
            set
            {
                unchecked
                {
                    MarkovMessageId = (long)value;
                }
            }
        }
    }
}
