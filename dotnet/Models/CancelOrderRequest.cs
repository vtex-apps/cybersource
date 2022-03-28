using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CancelOrderRequest
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
