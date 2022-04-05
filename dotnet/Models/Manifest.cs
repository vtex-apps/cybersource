using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class Manifest
    {
        [JsonProperty("paymentMethods")]
        public List<PaymentMethod> PaymentMethods { get; set; }

        [JsonProperty("customFields")]
        public List<CustomField> CustomFields { get; set; }
    }

    public class CustomField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        //[JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        //public List<Option> Options { get; set; }
    }

    public class Option
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class PaymentMethod
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("allowsSplit")]
        public string AllowsSplit { get; set; }
    }
}
