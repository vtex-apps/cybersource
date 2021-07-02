namespace Cybersource.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class PaymentMethodsList
    {
        [JsonProperty("paymentMethods")]
        public List<string> PaymentMethods { get; set; }
    }
}
