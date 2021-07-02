using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class TaxDetail
    {
        public string type { get; set; }
        public string amount { get; set; }
        public string rate { get; set; }
        public string code { get; set; }
        public string taxId { get; set; }
        public string applied { get; set; }
        public string exemptionCode { get; set; }
    }
}