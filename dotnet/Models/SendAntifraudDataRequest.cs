namespace Cybersource.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;

    public class SendAntifraudDataRequest
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("store")]
        public string Store { get; set; }

        [JsonProperty("deviceFingerprint")]
        public string DeviceFingerprint { get; set; }

        [JsonProperty("miniCart")]
        public AntifraudMiniCart MiniCart { get; set; }

        [JsonProperty("payments")]
        public AntifraudPayment[] Payments { get; set; }

        [JsonProperty("hook")]
        public Uri Hook { get; set; }

        [JsonProperty("transactionStartDate")]
        public DateTimeOffset TransactionStartDate { get; set; }

        [JsonProperty("merchantSettings")]
        public List<MerchantSetting> MerchantSettings { get; set; }
    }

    public class AntifraudMiniCart
    {
        [JsonProperty("buyer")]
        public AntifraudBuyer Buyer { get; set; }

        [JsonProperty("shipping")]
        public AntifraudShipping Shipping { get; set; }

        [JsonProperty("items")]
        public List<AntifraudItem> Items { get; set; }

        [JsonProperty("taxValue")]
        public decimal TaxValue { get; set; }

        [JsonProperty("listRegistry")]
        public ListRegistry ListRegistry { get; set; }
    }

    public class AntifraudBuyer
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("document")]
        public string Document { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("address")]
        public AntifraudAddress Address { get; set; }
    }

    public class AntifraudAddress
    {
        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("complement")]
        public string Complement { get; set; }

        [JsonProperty("neighborhood")]
        public string Neighborhood { get; set; }

        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }
    }

    public class AntifraudItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("quantity")]
        public long Quantity { get; set; }

        [JsonProperty("deliveryType")]
        public string DeliveryType { get; set; }

        [JsonProperty("deliverySlaInMinutes")]
        public long DeliverySlaInMinutes { get; set; }

        [JsonProperty("categoryId")]
        public string CategoryId { get; set; }

        [JsonProperty("categoryName")]
        public string CategoryName { get; set; }

        [JsonProperty("discount")]
        public double Discount { get; set; }

        [JsonProperty("sellerId")]
        public string SellerId { get; set; }
    }

    public class ListRegistry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deliveryToOwner")]
        public bool DeliveryToOwner { get; set; }
    }

    public class AntifraudShipping
    {
        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("estimatedDate")]
        public DateTimeOffset EstimatedDate { get; set; }

        [JsonProperty("address")]
        public AntifraudAddress Address { get; set; }
    }

    public class AntifraudPayment
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("currencyIso4217")]
        public string CurrencyIso4217 { get; set; }

        [JsonProperty("installments")]
        public long Installments { get; set; }

        [JsonProperty("details")]
        public Details Details { get; set; }
    }

    public class Details
    {
        [JsonProperty("bin")]
        public string Bin { get; set; }

        [JsonProperty("lastDigits")]
        public string LastDigits { get; set; }

        [JsonProperty("holder")]
        public string Holder { get; set; }

        [JsonProperty("address")]
        public AntifraudAddress Address { get; set; }
    }
}
