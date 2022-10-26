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

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ConsumerAuthenticationInformation
    {
        [JsonProperty("strongAuthentication")]
        public StrongAuthentication StrongAuthentication { get; set; }

        [JsonProperty("transactionMode")]
        public string TransactionMode { get; set; }

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("deviceDataCollectionUrl")]
        public string DeviceDataCollectionUrl { get; set; }

        [JsonProperty("referenceId")]
        public string ReferenceId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("authenticationTransactionId")]
        public string AuthenticationTransactionId { get; set; }

        [JsonProperty("eciRaw")]
        public string EciRaw { get; set; }

        [JsonProperty("eci")]
        public string Eci { get; set; }

        [JsonProperty("proofXml")]
        public string ProofXml { get; set; }

        [JsonProperty("cavv")]
        public string Cavv { get; set; }

        [JsonProperty("paresStatus")]
        public string ParesStatus { get; set; }

        [JsonProperty("xid")]
        public string Xid { get; set; }

        [JsonProperty("cavvAlgorithm")]
        public string CavvAlgorithm { get; set; }

        [JsonProperty("veresEnrolled")]
        public string VeresEnrolled { get; set; }

        [JsonProperty("authenticationPath")]
        public string AuthenticationPath { get; set; }

        [JsonProperty("ecommerceIndicator")]
        public string EcommerceIndicator { get; set; }

        [JsonProperty("specificationVersion")]
        public string SpecificationVersion { get; set; }

        [JsonProperty("authenticationType")]
        public string AuthenticationType { get; set; }

        [JsonProperty("acsWindowSize")]
        public string AcsWindowSize { get; set; }

        [JsonProperty("alternateAuthenticationData")]
        public string AlternateAuthenticationData { get; set; }

        [JsonProperty("alternateAuthenticationDate")]
        public string AlternateAuthenticationDate { get; set; }

        [JsonProperty("alternateAuthenticationMethod")]
        public string AlternateAuthenticationMethod { get; set; }

        [JsonProperty("authenticationDate")]
        public string AuthenticationDate { get; set; }

        [JsonProperty("transactionFlowIndicator")]
        public object TransactionFlowIndicator { get; set; }

        [JsonProperty("challengeCancelCode")]
        public string ChallengeCancelCode { get; set; }

        [JsonProperty("challengeCode")]
        public string ChallengeCode { get; set; }

        [JsonProperty("challengeStatus")]
        public string ChallengeStatus { get; set; }

        [JsonProperty("customerCardAlias")]
        public string CustomerCardAlias { get; set; }

        [JsonProperty("decoupledAuthenticationIndicator")]
        public string DecoupledAuthenticationIndicator { get; set; }

        [JsonProperty("decoupledAuthenticationMaxTime")]
        public string DecoupledAuthenticationMaxTime { get; set; }

        [JsonProperty("defaultCard")]
        public bool DefaultCard { get; set; }

        [JsonProperty("deviceChannel")]
        public string DeviceChannel { get; set; }

        [JsonProperty("installmentTotalCount")]
        public object InstallmentTotalCount { get; set; }

        [JsonProperty("merchantFraudRate")]
        public string MerchantFraudRate { get; set; }

        [JsonProperty("marketingOptIn")]
        public bool MarketingOptIn { get; set; }

        [JsonProperty("marketingSource")]
        public string MarketingSource { get; set; }

        [JsonProperty("mcc")]
        public string Mcc { get; set; }

        [JsonProperty("merchantScore")]
        public object MerchantScore { get; set; }

        [JsonProperty("messageCategory")]
        public string MessageCategory { get; set; }

        [JsonProperty("npaCode")]
        public string NpaCode { get; set; }

        [JsonProperty("overridePaymentMethod")]
        public string OverridePaymentMethod { get; set; }

        [JsonProperty("overrideCountryCode")]
        public string OverrideCountryCode { get; set; }

        [JsonProperty("priorAuthenticationData")]
        public string PriorAuthenticationData { get; set; }

        [JsonProperty("priorAuthenticationMethod")]
        public string PriorAuthenticationMethod { get; set; }

        [JsonProperty("priorAuthenticationReferenceId")]
        public string PriorAuthenticationReferenceId { get; set; }

        [JsonProperty("priorAuthenticationTime")]
        public string PriorAuthenticationTime { get; set; }

        [JsonProperty("productCode")]
        public string ProductCode { get; set; }

        [JsonProperty("returnUrl")]
        public string ReturnUrl { get; set; }

        [JsonProperty("requestorId")]
        public string RequestorId { get; set; }

        [JsonProperty("requestorInitiatedAuthenticationIndicator")]
        public string RequestorInitiatedAuthenticationIndicator { get; set; }

        [JsonProperty("requestorName")]
        public string RequestorName { get; set; }

        [JsonProperty("sdkMaxTimeout")]
        public string SdkMaxTimeout { get; set; }

        [JsonProperty("secureCorporatePaymentIndicator")]
        public string SecureCorporatePaymentIndicator { get; set; }

        [JsonProperty("whiteListStatus")]
        public string WhiteListStatus { get; set; }

        [JsonProperty("acsUrl")]
        public Uri AcsUrl { get; set; }

        [JsonProperty("stepUpUrl")]
        public Uri StepUpUrl { get; set; }

        [JsonProperty("pareq")]
        public string Pareq { get; set; }

        [JsonProperty("proxyPan")]
        public string ProxyPan { get; set; }

        [JsonProperty("signedParesStatusReason")]
        public string SignedParesStatusReason { get; set; }

        [JsonProperty("acsReferenceNumber")]
        public string AcsReferenceNumber { get; set; }

        [JsonProperty("ucafCollectionIndicator")]
        public string UcafCollectionIndicator { get; set; }

        [JsonProperty("directoryServerTransactionId")]
        public string DirectoryServerTransactionId { get; set; }

        [JsonProperty("threeDSServerTransactionId")]
        public Guid ThreeDsServerTransactionId { get; set; }

        [JsonProperty("acsOperatorID")]
        public string AcsOperatorId { get; set; }

        [JsonProperty("acsTransactionId")]
        public Guid AcsTransactionId { get; set; }
    }

    public partial class StrongAuthentication
    {
        [JsonProperty("authenticationIndicator")]
        public string AuthenticationIndicator { get; set; }

        [JsonProperty("OutageExemptionIndicator")]
        public string OutageExemptionIndicator { get; set; }
    }

    public class Profile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("decision")]
        public string Decision { get; set; }
    }

    public class ConsumerAuthenticationInformationWrapper : ConsumerAuthenticationInformation
    {
        [JsonProperty("createPaymentRequestReference")]
        public string CreatePaymentRequestReference { get; set; }

        public ConsumerAuthenticationInformationWrapper(ConsumerAuthenticationInformation consumerAuthenticationInformation)
        {
            StrongAuthentication = new StrongAuthentication
            {
                AuthenticationIndicator = consumerAuthenticationInformation.StrongAuthentication != null ? consumerAuthenticationInformation.StrongAuthentication.AuthenticationIndicator : null,
                OutageExemptionIndicator = consumerAuthenticationInformation.StrongAuthentication != null ? consumerAuthenticationInformation.StrongAuthentication.OutageExemptionIndicator : null
            };

            AccessToken = consumerAuthenticationInformation.AccessToken;
            AuthenticationPath = consumerAuthenticationInformation.AuthenticationPath;
            AuthenticationTransactionId = consumerAuthenticationInformation.AuthenticationTransactionId;
            Cavv = consumerAuthenticationInformation.Cavv;
            CavvAlgorithm = consumerAuthenticationInformation.CavvAlgorithm;
            DeviceDataCollectionUrl = consumerAuthenticationInformation.DeviceDataCollectionUrl;
            Eci = consumerAuthenticationInformation.Eci;
            EciRaw = consumerAuthenticationInformation.EciRaw;
            TransactionMode = consumerAuthenticationInformation.TransactionMode;
            ReferenceId = consumerAuthenticationInformation.ReferenceId;
            Token = consumerAuthenticationInformation.Token;
            ProofXml = consumerAuthenticationInformation.ProofXml;
            ParesStatus = consumerAuthenticationInformation.ParesStatus;
            Xid = consumerAuthenticationInformation.Xid;
            VeresEnrolled = consumerAuthenticationInformation.VeresEnrolled;
            AuthenticationPath = consumerAuthenticationInformation.AuthenticationPath;
            EcommerceIndicator = consumerAuthenticationInformation.EcommerceIndicator;
            SpecificationVersion = consumerAuthenticationInformation.SpecificationVersion;
        }
    }
}

