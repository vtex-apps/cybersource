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

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Value
    {
        [JsonProperty("sha256")]
        public Sha256 Sha256 { get; set; }

        [JsonProperty("hmac-sha256")]
        public HmacSha256 HmacSha256 { get; set; }
    }

    public class Sha256
    {
        [JsonProperty("replaceTokens")]
        public string[] ReplaceTokens { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HmacSha256
    {
        [JsonProperty("replaceTokens")]
        public string[] ReplaceTokens { get; set; }

        [JsonProperty("key")]
        public string[] Key { get; set; }

        [JsonProperty("data")]
        public string[] Data { get; set; }
    }
}
