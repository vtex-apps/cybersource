using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Cybersource.Models
{
    public class TransactionDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("referenceKey")]
        public string ReferenceKey { get; set; }

        [JsonProperty("interactions")]
        public Link Interactions { get; set; }

        [JsonProperty("settlements")]
        public Link Settlements { get; set; }

        [JsonProperty("payments")]
        public Link Payments { get; set; }

        [JsonProperty("refunds")]
        public Link Refunds { get; set; }

        [JsonProperty("cancellations")]
        public Link Cancellations { get; set; }

        [JsonProperty("capabilities")]
        public Link Capabilities { get; set; }

        [JsonProperty("timeoutStatus")]
        public long TimeoutStatus { get; set; }

        [JsonProperty("totalRefunds")]
        public long TotalRefunds { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }

        [JsonProperty("receiverUri")]
        public object ReceiverUri { get; set; }

        [JsonProperty("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("authorizationToken")]
        public object AuthorizationToken { get; set; }

        [JsonProperty("authorizationDate")]
        public object AuthorizationDate { get; set; }

        [JsonProperty("commitmentToken")]
        public object CommitmentToken { get; set; }

        [JsonProperty("commitmentDate")]
        public object CommitmentDate { get; set; }

        [JsonProperty("refundingToken")]
        public object RefundingToken { get; set; }

        [JsonProperty("refundingDate")]
        public object RefundingDate { get; set; }

        [JsonProperty("cancelationToken")]
        public string CancelationToken { get; set; }

        [JsonProperty("cancelationDate")]
        public DateTimeOffset CancelationDate { get; set; }

        [JsonProperty("fields")]
        public List<DetailField> Fields { get; set; }

        [JsonProperty("shopperInteraction")]
        public string ShopperInteraction { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("macId")]
        public string MacId { get; set; }

        [JsonProperty("vtexFingerprint")]
        public object VtexFingerprint { get; set; }

        [JsonProperty("chargeback")]
        public object Chargeback { get; set; }

        [JsonProperty("whiteSignature")]
        public object WhiteSignature { get; set; }

        [JsonProperty("owner")]
        public string Owner { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("userAgent")]
        public string UserAgent { get; set; }

        [JsonProperty("acceptHeader")]
        public string AcceptHeader { get; set; }

        [JsonProperty("antifraudTid")]
        public object AntifraudTid { get; set; }

        [JsonProperty("antifraudResponse")]
        public object AntifraudResponse { get; set; }

        [JsonProperty("antifraudReference")]
        public object AntifraudReference { get; set; }

        [JsonProperty("antifraudAffiliationId")]
        public string AntifraudAffiliationId { get; set; }

        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("salesChannel")]
        public string SalesChannel { get; set; }

        [JsonProperty("urn")]
        public object Urn { get; set; }

        [JsonProperty("softDescriptor")]
        public object SoftDescriptor { get; set; }

        [JsonProperty("markedForRecurrence")]
        public bool MarkedForRecurrence { get; set; }

        [JsonProperty("buyer")]
        public object Buyer { get; set; }
    }

    public class Link
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class DetailField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}

