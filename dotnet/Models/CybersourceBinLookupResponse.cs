using Newtonsoft.Json;
using System;

namespace Cybersource.Models
{
    public class CybersourceBinLookupResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("submitTimeUtc")]
        public DateTimeOffset SubmitTimeUtc { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("paymentAccountInformation")]
        public BinLookupPaymentAccountInformation PaymentAccountInformation { get; set; }

        [JsonProperty("issuerInformation")]
        public BinLookupIssuerInformation IssuerInformation { get; set; }
    }

    public class BinLookupIssuerInformation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    public class BinLookupPaymentAccountInformation
    {
        [JsonProperty("card")]
        public BinLookupResponseCard Card { get; set; }

        [JsonProperty("features")]
        public BinLookupFeatures Features { get; set; }
    }

    public class BinLookupResponseCard
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("brandName")]
        public string BrandName { get; set; }

        [JsonProperty("maxLength")]
        public string MaxLength { get; set; }
    }

    public class BinLookupFeatures
    {
        [JsonProperty("accountFundingSource")]
        public string AccountFundingSource { get; set; }

        [JsonProperty("cardPlatform")]
        public string CardPlatform { get; set; }

        [JsonProperty("cardProduct")]
        public string CardProduct { get; set; }
    }
}
