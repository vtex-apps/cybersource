using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Reversals
    {
        public ClientReferenceInformation clientReferenceInformation { get; set; }
        public ReversalInformation reversalInformation { get; set; }
    }
}