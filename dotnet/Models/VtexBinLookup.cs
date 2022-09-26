using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class VtexBinLookup
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("cardBrand")]
        public string CardBrand { get; set; }

        [JsonProperty("cardCoBrand")]
        public string CardCoBrand { get; set; }

        [JsonProperty("cardType")]
        public string CardType { get; set; }

        [JsonProperty("country")]
        public BinCountry Country { get; set; }

        [JsonProperty("bank")]
        public BinBank Bank { get; set; }

        [JsonProperty("cvvSize")]
        public string CvvSize { get; set; }

        [JsonProperty("expirable")]
        public bool Expirable { get; set; }

        [JsonProperty("validationAlgorithm")]
        public string ValidationAlgorithm { get; set; }

        [JsonProperty("additionalInfo")]
        public string AdditionalInfo { get; set; }

        [JsonProperty("cardLevel")]
        public string CardLevel { get; set; }
    }

    public class BinBank
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }

    public class BinCountry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("isoCode")]
        public string IsoCode { get; set; }

        [JsonProperty("isoCodeThreeDigits")]
        public string IsoCodeThreeDigits { get; set; }
    }
}
