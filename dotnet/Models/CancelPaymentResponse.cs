using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CancelPaymentResponse
    {
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("cancellationId")]
        public string CancellationId { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
    }
}
