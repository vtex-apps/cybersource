using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cybersource.Models
{
    public class RetrieveTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("submitTimeUTC")]
        public DateTimeOffset SubmitTimeUtc { get; set; }

        [JsonProperty("merchantId")]
        public string MerchantId { get; set; }

        [JsonProperty("applicationInformation")]
        public ApplicationInformation ApplicationInformation { get; set; }

        [JsonProperty("buyerInformation")]
        public BuyerInformation BuyerInformation { get; set; }

        [JsonProperty("clientReferenceInformation")]
        public ClientReferenceInformation ClientReferenceInformation { get; set; }

        [JsonProperty("consumerAuthenticationInformation")]
        public ConsumerAuthenticationInformation ConsumerAuthenticationInformation { get; set; }

        [JsonProperty("deviceInformation")]
        public DeviceInformation DeviceInformation { get; set; }

        [JsonProperty("installmentInformation")]
        public BuyerInformation InstallmentInformation { get; set; }

        [JsonProperty("fraudMarkingInformation")]
        public BuyerInformation FraudMarkingInformation { get; set; }

        [JsonProperty("merchantDefinedInformation")]
        public List<MerchantDefinedInformation> MerchantDefinedInformation { get; set; }

        [JsonProperty("merchantInformation")]
        public MerchantInformation MerchantInformation { get; set; }

        [JsonProperty("orderInformation")]
        public OrderInformation OrderInformation { get; set; }

        [JsonProperty("paymentInformation")]
        public PaymentInformation PaymentInformation { get; set; }

        [JsonProperty("processingInformation")]
        public ProcessingInformation ProcessingInformation { get; set; }

        [JsonProperty("processorInformation")]
        public ProcessorInformation ProcessorInformation { get; set; }

        [JsonProperty("pointOfSaleInformation")]
        public BuyerInformation PointOfSaleInformation { get; set; }

        [JsonProperty("riskInformation")]
        public RiskInformation RiskInformation { get; set; }

        [JsonProperty("recipientInformation")]
        public BuyerInformation RecipientInformation { get; set; }

        [JsonProperty("senderInformation")]
        public BuyerInformation SenderInformation { get; set; }

        [JsonProperty("tokenInformation")]
        public BuyerInformation TokenInformation { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }

    public class ApplicationInformation
    {
        [JsonProperty("reasonCode")]
        public long ReasonCode { get; set; }

        [JsonProperty("applications")]
        public List<Application> Applications { get; set; }
    }

    public class Application
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("reasonCode")]
        public string ReasonCode { get; set; }

        [JsonProperty("rCode")]
        public string RCode { get; set; }

        [JsonProperty("rFlag", NullValueHandling = NullValueHandling.Ignore)]
        public string RFlag { get; set; }

        [JsonProperty("rMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string RMessage { get; set; }

        [JsonProperty("returnCode")]
        public long ReturnCode { get; set; }
    }

    public class ConsumerAuthenticationInformation
    {
        [JsonProperty("strongAuthentication")]
        public BuyerInformation StrongAuthentication { get; set; }
    }

    public class Profile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("decision")]
        public string Decision { get; set; }
    }
}

