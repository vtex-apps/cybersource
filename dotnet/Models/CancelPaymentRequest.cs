using Newtonsoft.Json;

namespace Cybersource.Models
{   
    public class CancelPaymentRequest
    {
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("authorizationId")]
        public string AuthorizationId { get; set; }
    }
}
