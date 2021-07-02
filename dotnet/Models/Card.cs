using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Card
    {
        public string expirationYear { get; set; }

        public string number { get; set; }

        public string securityCode { get; set; }

        public string expirationMonth { get; set; }

        public string type { get; set; }
    }
}