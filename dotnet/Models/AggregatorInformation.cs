using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AggregatorInformation
    {
        public SubMerchant subMerchant { get; set; }

        public string name { get; set; }

        public string aggregatorID { get; set; }
    }
}