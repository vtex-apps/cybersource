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
    }
}
