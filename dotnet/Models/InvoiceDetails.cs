using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class InvoiceDetails
    {
        public string invoiceNumber { get; set; }
        public string barcodeNumber { get; set; }
        public string expirationDate { get; set; }
        public string purchaseOrderNumber { get; set; }
        public string purchaseOrderDate { get; set; }
        public string purchaseContactName { get; set; }
        public bool taxable { get; set; }
        public string vatInvoiceReferenceNumber { get; set; }
        public string commodityCode { get; set; }
        public object merchandiseCode { get; set; }
        public List<TransactionAdviceAddendum> transactionAdviceAddendum { get; set; }
        public string referenceDataCode { get; set; }
        public string referenceDataNumber { get; set; }
        public object salesSlipNumber { get; set; }
        public string invoiceDate { get; set; }
        public string costCenter { get; set; }
        public string issuerMessage { get; set; }
    }

    public class TransactionAdviceAddendum
    {
        public string data { get; set; }
    }
}
