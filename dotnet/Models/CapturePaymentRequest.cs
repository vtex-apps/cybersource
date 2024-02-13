using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cybersource.Models
{
    public class CapturePaymentRequest
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("authorizationId")]
        public string AuthorizationId { get; set; }

        [JsonProperty("merchantSettings")]
        public List<MerchantSetting> MerchantSettings { get; set; }
    }
}
