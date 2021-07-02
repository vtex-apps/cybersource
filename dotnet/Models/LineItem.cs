using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class LineItem
    {
        public string productCode { get; set; }
        public string productName { get; set; }
        public string productSku { get; set; }
        public string quantity { get; set; }
        public string unitPrice { get; set; }
        public string unitOfMeasure { get; set; }
        public string totalAmount { get; set; }
        public string taxAmount { get; set; }
        public string taxRate { get; set; }
        public string taxAppliedAfterDiscount { get; set; }
        public string taxStatusIndicator { get; set; }
        public string taxTypeCode { get; set; }
        public string amountIncludesTax { get; set; }
        public string typeOfSupply { get; set; }
        public string commodityCode { get; set; }
        public string discountAmount { get; set; }
        public string discountApplied { get; set; }
        public string discountRate { get; set; }
        public string invoiceNumber { get; set; }
        public TaxDetail[] taxDetails { get; set; }
        public string fulfillmentType { get; set; }
        public string weight { get; set; }
        public string weightIdentifier { get; set; }
        public string weightUnit { get; set; }
        public string referenceDataCode { get; set; }
        public string referenceDataNumber { get; set; }
        public string productDescription { get; set; }
        public string giftCardCurrency { get; set; }
        public string shippingDestinationTypes { get; set; }
        public string gift { get; set; }
        //public Passenger passenger { get; set; }
    }
}