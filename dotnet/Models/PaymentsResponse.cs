namespace Cybersource.Models
{
    using System;
    using Newtonsoft.Json;

    public class PaymentsResponse
    {
        [JsonProperty("_links")]
        public Links Links { get; set; }

        [JsonProperty("clientReferenceInformation")]
        public ClientReferenceInformation ClientReferenceInformation { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("orderInformation")]
        public OrderInformation OrderInformation { get; set; }

        [JsonProperty("paymentAccountInformation")]
        public PaymentAccountInformation PaymentAccountInformation { get; set; }

        [JsonProperty("paymentInformation")]
        public PaymentInformation PaymentInformation { get; set; }

        [JsonProperty("pointOfSaleInformation")]
        public PointOfSaleInformation PointOfSaleInformation { get; set; }

        [JsonProperty("processorInformation")]
        public ProcessorInformation ProcessorInformation { get; set; }

        [JsonProperty("reconciliationId")]
        public string ReconciliationId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("submitTimeUtc")]
        public DateTimeOffset SubmitTimeUtc { get; set; }

        [JsonProperty("details")]
        public Detail[] Details { get; set; }

        [JsonProperty("errorInformation")]
        public ErrorInformation ErrorInformation { get; set; }

        [JsonProperty("refundAmountDetails")]
        public RefundAmountDetails RefundAmountDetails { get; set; }

        [JsonProperty("riskInformation")]
        public RiskInformation RiskInformation { get; set; }
    }

    public class Links
    {
        [JsonProperty("authReversal")]
        public AuthReversal AuthReversal { get; set; }

        [JsonProperty("self")]
        public AuthReversal Self { get; set; }

        [JsonProperty("capture")]
        public AuthReversal Capture { get; set; }
    }

    public class AuthReversal
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class PaymentAccountInformation
    {
        [JsonProperty("card")]
        public Card Card { get; set; }
    }

    public class PointOfSaleInformation
    {
        [JsonProperty("terminalId")]
        public string TerminalId { get; set; }
    }

    public class ProcessorInformation
    {
        [JsonProperty("approvalCode")]
        public string ApprovalCode { get; set; }

        [JsonProperty("networkTransactionId")]
        public string NetworkTransactionId { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("responseCode")]
        public string ResponseCode { get; set; }

        [JsonProperty("avs")]
        public Avs Avs { get; set; }
    }

    public class Avs
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("codeRaw")]
        public string CodeRaw { get; set; }
    }

    public class Detail
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class ErrorInformation
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class RefundAmountDetails
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("refundAmount")]
        public string RefundAmount { get; set; }
    }

    public class RiskInformation
    {
        [JsonProperty("localTime")]
        public DateTimeOffset LocalTime { get; set; }

        [JsonProperty("score")]
        public Score Score { get; set; }

        [JsonProperty("infoCodes")]
        public InfoCodes InfoCodes { get; set; }
    }

    public class InfoCodes
    {
        [JsonProperty("address")]
        public string[] Address { get; set; }

        [JsonProperty("phone")]
        public string[] Phone { get; set; }

        [JsonProperty("globalVelocity")]
        public string[] GlobalVelocity { get; set; }

        [JsonProperty("identityChange")]
        public string[] IdentityChange { get; set; }
    }

    public class Score
    {
        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("factorCodes")]
        public string[] FactorCodes { get; set; }

        [JsonProperty("modelUsed")]
        public string ModelUsed { get; set; }
    }
}
