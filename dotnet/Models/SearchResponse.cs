using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cybersource.Models
{
    public class SearchResponse
    {
        [JsonProperty("_links")]
        public SearchResponseLinks Links { get; set; }

        [JsonProperty("searchId")]
        public Guid SearchId { get; set; }

        [JsonProperty("save")]
        public bool Save { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("count")]
        public long Count { get; set; }

        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }

        [JsonProperty("offset")]
        public long Offset { get; set; }

        [JsonProperty("sort")]
        public string Sort { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("submitTimeUtc")]
        public DateTimeOffset SubmitTimeUtc { get; set; }

        [JsonProperty("_embedded")]
        public Embedded Embedded { get; set; }
    }

    public class Embedded
    {
        [JsonProperty("transactionSummaries")]
        public List<TransactionSummary> TransactionSummaries { get; set; }
    }

    public class TransactionSummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("submitTimeUtc")]
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
        public BuyerInformation DeviceInformation { get; set; }

        [JsonProperty("fraudMarkingInformation")]
        public BuyerInformation FraudMarkingInformation { get; set; }

        [JsonProperty("merchantInformation")]
        public BuyerInformation MerchantInformation { get; set; }

        [JsonProperty("orderInformation")]
        public OrderInformation OrderInformation { get; set; }

        [JsonProperty("paymentInformation")]
        public PaymentInformation PaymentInformation { get; set; }

        [JsonProperty("processingInformation")]
        public ProcessingInformation ProcessingInformation { get; set; }

        [JsonProperty("processorInformation")]
        public ProcessorInformation ProcessorInformation { get; set; }

        [JsonProperty("pointOfSaleInformation")]
        public PointOfSaleInformation PointOfSaleInformation { get; set; }

        [JsonProperty("riskInformation")]
        public RiskInformation RiskInformation { get; set; }

        [JsonProperty("_links")]
        public TransactionSummaryLinks Links { get; set; }

        [JsonProperty("installmentInformation")]
        public BuyerInformation InstallmentInformation { get; set; }
    }

    public class TransactionSummaryLinks
    {
        [JsonProperty("transactionDetail")]
        public Self TransactionDetail { get; set; }
    }

    public class To
    {
        [JsonProperty("address1", NullValueHandling = NullValueHandling.Ignore)]
        public string Address1 { get; set; }

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State { get; set; }

        [JsonProperty("city", NullValueHandling = NullValueHandling.Ignore)]
        public string City { get; set; }

        [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
        public string Country { get; set; }

        [JsonProperty("postalCode", NullValueHandling = NullValueHandling.Ignore)]
        public string PostalCode { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; set; }

        [JsonProperty("phoneNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string PhoneNumber { get; set; }

        [JsonProperty("firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; set; }

        [JsonProperty("lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; set; }
    }

    public class Account
    {
        [JsonProperty("suffix")]
        public string Suffix { get; set; }
    }

    public class Customer
    {
        [JsonProperty("customerId", NullValueHandling = NullValueHandling.Ignore)]
        public string CustomerId { get; set; }
    }

    public class PaymentType
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }
    }

    public class AuthorizationOptions
    {
        [JsonProperty("authIndicator")]
        public string AuthIndicator { get; set; }
    }

    public class Processor
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    public class Providers
    {
        [JsonProperty("fingerPrint")]
        public BuyerInformation FingerPrint { get; set; }
    }

    public class SearchResponseLinks
    {
        [JsonProperty("self")]
        public Self Self { get; set; }
    }
}

