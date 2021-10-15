using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class BinLookup
    {
        [JsonProperty("number")]
        public Number Number { get; set; }

        [JsonProperty("scheme")]
        public string Scheme { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("country")]
        public Country Country { get; set; }

        [JsonProperty("bank")]
        public Bank Bank { get; set; }
    }

    public class Bank
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }

    public class Country
    {
        [JsonProperty("numeric")]
        public string Numeric { get; set; }

        [JsonProperty("alpha2")]
        public string Alpha2 { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("emoji")]
        public string Emoji { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("latitude")]
        public long Latitude { get; set; }

        [JsonProperty("longitude")]
        public long Longitude { get; set; }
    }

    public class Number
    {
        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("luhn")]
        public bool Luhn { get; set; }
    }
}

