using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class DeviceInformation
    {
        public string cookiesAccepted { get; set; }
        public string ipAddress { get; set; }
        public string hostName { get; set; }
        public string fingerprintSessionId { get; set; }
        public string httpBrowserEmail { get; set; }
        public string userAgent { get; set; }
    }
}