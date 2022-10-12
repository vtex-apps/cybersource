using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AmountDetails
    {
        public string authorizedAmount { get; set; }
        public string totalAmount { get; set; }
        public string currency { get; set; }
        public string taxAmount { get; set; }
        public string freightAmount { get; set; }
        public string nationalTaxIncluded { get; set; }
    }
}