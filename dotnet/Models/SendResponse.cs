using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class SendResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string StatusCode { get; set; }
    }
}
