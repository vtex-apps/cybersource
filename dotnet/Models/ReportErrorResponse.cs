using Newtonsoft.Json;
using System;

namespace Cybersource.Models
{
    public class ReportErrorResponse
    {
        [JsonProperty("_links")]
        public ErrorLinks Links { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("correlationId")]
        public object CorrelationId { get; set; }

        [JsonProperty("detail")]
        public object Detail { get; set; }

        [JsonProperty("fields")]
        public Field[] Fields { get; set; }

        [JsonProperty("localizationKey")]
        public string LocalizationKey { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class Field
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("localizationKey")]
        public object LocalizationKey { get; set; }
    }

    public class ErrorLinks
    {
        [JsonProperty("self")]
        public ErrorSelf Self { get; set; }
    }

    public class ErrorSelf
    {
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }
}
