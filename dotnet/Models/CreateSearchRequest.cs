using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CreateSearchRequest
    {
        //[JsonProperty("save")]
        //public string Save { get; set; }

        //[JsonProperty("name")]
        //public string Name { get; set; }

        //[JsonProperty("timezone")]
        //public string Timezone { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        //[JsonProperty("offset")]
        //public long Offset { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("sort")]
        public string Sort { get; set; }
    }
}
