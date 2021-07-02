using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AmountDetails
    {
        public string authorizedAmount { get; set; }
        public string totalAmount { get; set; }

        public string currency { get; set; }
    }
}