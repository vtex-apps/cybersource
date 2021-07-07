using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cybersource.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class OrderInformation
    {
        public BillTo billTo { get; set; }
        public ShipTo shipTo { get; set; }
        public AmountDetails amountDetails { get; set; }
        public List<LineItem> lineItems { get; set; }
    }
}