using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ClientReferenceInformation
    {
        public string code { get; set; }
        public string transactionId { get; set; }
        public string comments { get; set; }
        public string applicationName { get; set; }
        public string applicationVersion { get; set; }
        public string applicationUser { get; set; }
    }
}