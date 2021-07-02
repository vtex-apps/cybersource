using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ReversalInformation
    {
        public AmountDetails amountDetails { get; set; }
        public string reason { get; set; }
    }
}