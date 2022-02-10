using Newtonsoft.Json;
using System.Collections.Generic;

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
        //[JsonProperty("sha256")]
        //public Sha256 Sha256 { get; set; }

        [JsonProperty("sha256")]
        public object[] Sha256 { get; set; }

        [JsonProperty("hmac-sha256")]
        public object[] HmacSha256 { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Sha256
    {
        [JsonProperty("replaceTokens")]
        public string[] ReplaceTokens { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class HmacSha256Class
    {
        [JsonProperty("replaceTokens")]
        public string[] ReplaceTokens { get; set; }
    }
}
