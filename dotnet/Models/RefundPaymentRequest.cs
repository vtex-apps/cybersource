using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cybersource.Models
{
    public class RefundPaymentRequest
    {
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        [JsonProperty("settleId")]
        public string SettleId { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("merchantSettings")]
        public List<MerchantSetting> MerchantSettings { get; set; }
    }
}
