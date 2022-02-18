using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class MerchantSettings
    {
        [JsonProperty("isLive")]
        public bool IsLive { get; set; }

        [JsonProperty("merchantId")]
        public string MerchantId { get; set; }

        [JsonProperty("merchantKey")]
        public string MerchantKey { get; set; }

        [JsonProperty("sharedSecretKey")]
        public string SharedSecretKey { get; set; }

        [JsonProperty("processor")]
        public string Processor { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        // Taxes
        [JsonProperty("enableTax")]
        public bool EnableTax { get; set; }

        [JsonProperty("enableTransactionPosting")]
        public bool EnableTransactionPosting { get; set; }

        [JsonProperty("salesChannelExclude")]
        public string SalesChannelExclude { get; set; }

        [JsonProperty("merchantDefinedValues")]
        public Dictionary<int, string> MerchantDefinedValues { get; set; }
    }
}