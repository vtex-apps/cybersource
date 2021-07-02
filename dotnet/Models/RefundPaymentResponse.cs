using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class RefundPaymentResponse
    {
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("refundId")]
        public string RefundId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
    }
}
