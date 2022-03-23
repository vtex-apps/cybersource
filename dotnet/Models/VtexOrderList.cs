using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class VtexOrderList
    {
        [JsonProperty("list")]
        public List<List> List { get; set; }

        [JsonProperty("facets")]
        public List<object> Facets { get; set; }

        [JsonProperty("paging")]
        public Paging Paging { get; set; }

        [JsonProperty("stats")]
        public VtexOrderListStats Stats { get; set; }

        [JsonProperty("reportRecordsLimit")]
        public long ReportRecordsLimit { get; set; }
    }

    public class List
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("creationDate")]
        public DateTimeOffset CreationDate { get; set; }

        [JsonProperty("clientName")]
        public string ClientName { get; set; }

        [JsonProperty("items")]
        public object Items { get; set; }

        [JsonProperty("totalValue")]
        public long TotalValue { get; set; }

        [JsonProperty("paymentNames")]
        public string PaymentNames { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusDescription")]
        public string StatusDescription { get; set; }

        [JsonProperty("marketPlaceOrderId")]
        public object MarketPlaceOrderId { get; set; }

        [JsonProperty("sequence")]
        public string Sequence { get; set; }

        [JsonProperty("salesChannel")]
        public string SalesChannel { get; set; }

        [JsonProperty("affiliateId")]
        public string AffiliateId { get; set; }

        [JsonProperty("origin")]
        public string Origin { get; set; }

        [JsonProperty("workflowInErrorState")]
        public bool WorkflowInErrorState { get; set; }

        [JsonProperty("workflowInRetry")]
        public bool WorkflowInRetry { get; set; }

        [JsonProperty("lastMessageUnread")]
        public string LastMessageUnread { get; set; }

        [JsonProperty("ShippingEstimatedDate")]
        public DateTimeOffset ShippingEstimatedDate { get; set; }

        [JsonProperty("ShippingEstimatedDateMax")]
        public DateTimeOffset ShippingEstimatedDateMax { get; set; }

        [JsonProperty("ShippingEstimatedDateMin")]
        public DateTimeOffset ShippingEstimatedDateMin { get; set; }

        [JsonProperty("orderIsComplete")]
        public bool OrderIsComplete { get; set; }

        [JsonProperty("listId")]
        public object ListId { get; set; }

        [JsonProperty("listType")]
        public object ListType { get; set; }

        [JsonProperty("authorizedDate")]
        public DateTimeOffset AuthorizedDate { get; set; }

        [JsonProperty("callCenterOperatorName")]
        public string CallCenterOperatorName { get; set; }

        [JsonProperty("totalItems")]
        public long TotalItems { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("invoiceOutput")]
        public object InvoiceOutput { get; set; }

        [JsonProperty("invoiceInput")]
        public object InvoiceInput { get; set; }

        [JsonProperty("lastChange")]
        public DateTimeOffset LastChange { get; set; }

        [JsonProperty("isAllDelivered")]
        public bool IsAllDelivered { get; set; }

        [JsonProperty("isAnyDelivered")]
        public bool IsAnyDelivered { get; set; }

        [JsonProperty("giftCardProviders")]
        public object GiftCardProviders { get; set; }

        [JsonProperty("orderFormId")]
        public string OrderFormId { get; set; }

        [JsonProperty("paymentApprovedDate")]
        public DateTimeOffset PaymentApprovedDate { get; set; }

        [JsonProperty("readyForHandlingDate")]
        public DateTimeOffset ReadyForHandlingDate { get; set; }

        [JsonProperty("deliveryDates")]
        public object DeliveryDates { get; set; }
    }

    public class Paging
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("pages")]
        public long Pages { get; set; }

        [JsonProperty("currentPage")]
        public long CurrentPage { get; set; }

        [JsonProperty("perPage")]
        public long PerPage { get; set; }
    }

    public class VtexOrderListStats
    {
        [JsonProperty("stats")]
        public StatsStats Stats { get; set; }
    }

    public class StatsStats
    {
        [JsonProperty("totalValue")]
        public StatsTotal TotalValue { get; set; }

        [JsonProperty("totalItems")]
        public StatsTotal TotalItems { get; set; }
    }

    public class StatsTotal
    {
        [JsonProperty("Count")]
        public long Count { get; set; }

        [JsonProperty("Max")]
        public long Max { get; set; }

        [JsonProperty("Mean")]
        public long Mean { get; set; }

        [JsonProperty("Min")]
        public long Min { get; set; }

        [JsonProperty("Missing")]
        public long Missing { get; set; }

        [JsonProperty("StdDev")]
        public long StdDev { get; set; }

        [JsonProperty("Sum")]
        public long Sum { get; set; }

        [JsonProperty("SumOfSquares")]
        public long SumOfSquares { get; set; }

        [JsonProperty("Facets")]
        public Facets Facets { get; set; }
    }

    public partial class Facets
    {
    }
}
