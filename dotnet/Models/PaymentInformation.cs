using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class PaymentInformation
    {
        public Card card { get; set; }
    }
}