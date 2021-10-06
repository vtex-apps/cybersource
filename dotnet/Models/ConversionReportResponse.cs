using Newtonsoft.Json;
using System;

namespace Cybersource.Models
{
    public class ConversionReportResponse
    {
        [JsonProperty("_links")]
        public ReportLinks Links { get; set; }

        [JsonProperty("conversionDetails")]
        public ConversionDetail[] ConversionDetails { get; set; }

        [JsonProperty("endTime")]
        public DateTimeOffset EndTime { get; set; }

        [JsonProperty("organizationId")]
        public string OrganizationId { get; set; }

        [JsonProperty("startTime")]
        public DateTimeOffset StartTime { get; set; }
    }

    public class ConversionDetail
    {
        [JsonProperty("merchantReferenceNumber")]
        public string MerchantReferenceNumber { get; set; }

        [JsonProperty("conversionTime")]
        public DateTimeOffset ConversionTime { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("originalDecision")]
        public string OriginalDecision { get; set; }

        [JsonProperty("newDecision")]
        public string NewDecision { get; set; }

        [JsonProperty("reviewer")]
        public string Reviewer { get; set; }

        [JsonProperty("reviewerComments")]
        public string ReviewerComments { get; set; }

        [JsonProperty("queue")]
        public string Queue { get; set; }

        [JsonProperty("profile")]
        public string Profile { get; set; }

        [JsonProperty("notes")]
        public Note[] Notes { get; set; }
    }

    public class Note
    {
        [JsonProperty("time")]
        public DateTimeOffset Time { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("addedBy")]
        public string AddedBy { get; set; }
    }

    public class ReportLinks
    {
        [JsonProperty("self")]
        public Self Self { get; set; }
    }

    public class Self
    {
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }
}

