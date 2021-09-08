using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class VtexOrderformTaxConfiguration
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        //[JsonProperty("authorizationHeader")]
        //public string AuthorizationHeader { get; set; }

        [JsonProperty("allowExecutionAfterErrors")]
        public bool AllowExecutionAfterErrors { get; set; }

        [JsonProperty("integratedAuthentication")]
        public bool IntegratedAuthentication { get; set; }

        [JsonProperty("appId")]
        public string AppId { get; set; }
    }
}
