using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProcessingInformation
    {
        public string commerceIndicator { get; set; }
        public string capture { get; set; }
        public string reconciliationId { get; set; }
    }
}