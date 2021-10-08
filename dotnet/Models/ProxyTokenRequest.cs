using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class ProxyTokenRequest
    {
        [JsonProperty("tokens")]
        public RequestToken[] Tokens { get; set; }
    }

    public class RequestToken
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public Value Value { get; set; }
    }

    public class Value
    {
        [JsonProperty("sha256")]
        public Sha256 Sha256 { get; set; }
    }

    public class Sha256
    {
        [JsonProperty("replaceTokens")]
        public string[] ReplaceTokens { get; set; }
    }
}
