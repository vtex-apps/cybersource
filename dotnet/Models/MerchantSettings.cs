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

        [JsonProperty("usePayerAuthentication")]
        public bool UsePayerAuthentication { get; set; }

        // Taxes
        [JsonProperty("enableTax")]
        public bool EnableTax { get; set; }

        [JsonProperty("enableTransactionPosting")]
        public bool EnableTransactionPosting { get; set; }

        [JsonProperty("salesChannelExclude")]
        public string SalesChannelExclude { get; set; }

        [JsonProperty("shippingProductCode")]
        public string ShippingProductCode { get; set; }

        [JsonProperty("nexusRegions")]
        public string NexusRegions { get; set; }

        // Merchant Defined Fields
        [JsonProperty("merchantDictionary")]
        public List<MerchantDefinedValueSetting> MerchantDefinedValueSettings { get; set; }
    }

    public class MerchantDefinedValueSetting
    {
        [JsonProperty("userInput")]
        public string UserInput { get; set; }

        [JsonProperty("goodPortion")]
        public string GoodPortion { get; set; }

        [JsonProperty("isValid")]
        public bool IsValid { get; set; }
    }
}