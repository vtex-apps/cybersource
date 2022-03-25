using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CybersourceBinLookupRequest
    {
        [JsonProperty("paymentInformation")]
        public BinLookupPaymentInformation PaymentInformation { get; set; }
    }

    public class BinLookupPaymentInformation
    {
        [JsonProperty("card")]
        public BinLookupCard Card { get; set; }
    }

    public class BinLookupCard
    {
        [JsonProperty("number")]
        public string Number { get; set; }
    }
}
