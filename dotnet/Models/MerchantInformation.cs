using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class MerchantInformation
    {
        public MerchantDescriptor merchantDescriptor { get; set; }
        public string salesOrganizationId { get; set; }
        public object categoryCode { get; set; }
        public object categoryCodeDomestic { get; set; }
        public string taxId { get; set; }
        public string vatRegistrationNumber { get; set; }
        public string cardAcceptorReferenceNumber { get; set; }
        public string transactionLocalDateTime { get; set; }
        public ServiceFeeDescriptor serviceFeeDescriptor { get; set; }
        public string merchantName { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class MerchantDescriptor
    {
        public string name { get; set; }
        public string alternateName { get; set; }
        public string contact { get; set; }
        public string address1 { get; set; }
        public string locality { get; set; }
        public string country { get; set; }
        public string postalCode { get; set; }
        public string administrativeArea { get; set; }
        public string phone { get; set; }
        public string url { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ServiceFeeDescriptor
    {
        public string name { get; set; }
        public string contact { get; set; }
        public string state { get; set; }
    }
}
