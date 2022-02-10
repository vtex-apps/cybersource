using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class MerchantSettings
    {
        public bool IsLive { get; set; }
        public string MerchantId { get; set; }
        public string MerchantKey { get; set; }
        public string SharedSecretKey { get; set; }
        public string Processor { get; set; }
        public string Region { get; set; }
        // Taxes
        public bool EnableTax { get; set; }
        public bool EnableTransactionPosting { get; set; }
        public string SalesChannelExclude { get; set; }
        //  merchantInformation moved to configuration.json
        //public string merchantName { get; set; }
        //public string merchantTaxId { get; set; }
        public Dictionary<int, string> MerchantDefinedValues { get; set; }
    }
}