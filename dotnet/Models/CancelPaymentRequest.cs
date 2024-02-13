using Newtonsoft.Json;
using System.Collections.Generic;

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

        [JsonProperty("merchantSettings")]
        public List<MerchantSetting> MerchantSettings { get; set; }
    }
}
