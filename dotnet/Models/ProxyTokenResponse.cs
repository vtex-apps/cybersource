using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class ProxyTokenResponse
    {
        [JsonProperty("tokens")]
        public ResponseToken[] Tokens { get; set; }
    }

    public class ResponseToken
    {
        [JsonProperty("name")]
        public object Name { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }
    }
}
