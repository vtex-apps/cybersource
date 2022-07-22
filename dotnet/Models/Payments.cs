using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Payments
    {
        public ClientReferenceInformation clientReferenceInformation { get; set; }
        public ProcessingInformation processingInformation { get; set; }
        public AggregatorInformation aggregatorInformation { get; set; }
        public OrderInformation orderInformation { get; set; }
        public PaymentInformation paymentInformation { get; set; }
        public ReversalInformation reversalInformation { get; set; }
        public DeviceInformation deviceInformation { get; set; }
        public InstallmentInformation installmentInformation { get; set; }
        public BuyerInformation buyerInformation { get; set; }
        public IssuerInformation issuerInformation { get; set; }
        public TaxInformation taxInformation { get; set; }
        public MerchantInformation merchantInformation { get; set; }
        public List<MerchantDefinedInformation> merchantDefinedInformation { get; set; }
        public ConsumerAuthenticationInformation ConsumerAuthenticationInformation { get; set; }
    }
}