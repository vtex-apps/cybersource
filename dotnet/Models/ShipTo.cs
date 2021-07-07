using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ShipTo
    {
        public string address1 { get; set; }

        public string address2 { get; set; }

        public string administrativeArea { get; set; }

        public string country { get; set; }

        public string destinationTypes { get; set; }

        public string locality { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        public string phoneNumber { get; set; }

        public string postalCode { get; set; }

        public string destinationCode { get; set; }

        public string method { get; set; }
    }
}