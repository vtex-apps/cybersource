namespace Cybersource.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class SendAntifraudDataResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tid")]
        public string Tid { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("analysisType")]
        public string AnalysisType { get; set; }

        [JsonProperty("responses")]
        public Dictionary<string, string> Responses { get; set; }
    }
}
