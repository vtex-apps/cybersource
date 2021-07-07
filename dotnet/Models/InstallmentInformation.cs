using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class InstallmentInformation
    {
        public string amount { get; set; }
        public string frequency { get; set; }
        public string planType { get; set; }
        public string sequence { get; set; }
        public string totalAmount { get; set; }
        public string totalCount { get; set; }
        public string firstInstallmentDate { get; set; }
        public string invoiceData { get; set; }
        public string paymentType { get; set; }
        public string eligibilityInquiry { get; set; }
        public string gracePeriodDuration { get; set; }
        public string gracePeriodDurationType { get; set; }
        public string firstInstallmentAmount { get; set; }
    }
}