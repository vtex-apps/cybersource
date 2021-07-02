using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CapturePaymentResponse
    {
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("settleId")]
        public string SettleId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("code")]
        public object Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }
    }
}
