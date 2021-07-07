using Newtonsoft.Json;

namespace Cybersource.Models
{
    using System.Collections.Generic;

    public class CreatePaymentRequest
    {
        /// <summary>
        /// Merchant's order reference
        /// </summary>
        [JsonProperty("reference")]
        public string Reference { get; set; }

        /// <summary>
        /// VTEX transaction ID related to this payment
        /// </summary>
        [JsonProperty("transactionId")]
        public string TransactionId { get; set; }

        /// <summary>
        /// VTEX payment ID that can be use as an unique identifier
        /// </summary>
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        /// <summary>
        /// One of the available payment methods
        /// </summary>
        [JsonProperty("paymentMethod")]
        public string PaymentMethod { get; set; }

        /// <summary>
        /// VTEX merchant name that will receive the payment
        /// </summary>
        [JsonProperty("merchantName")]
        public string MerchantName { get; set; }

        /// <summary>
        /// Value amount of the payment
        /// </summary>
        [JsonProperty("value")]
        public decimal Value { get; set; }

        /// <summary>
        /// ISO 4217 "Alpha-3" currency code
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Number of installments
        /// </summary>
        [JsonProperty("installments")]
        public int Installments { get; set; }

        /// <summary>
        /// The interest rate
        /// </summary>
        [JsonProperty("installmentsInterestRate")]
        public int InstallmentsInterestRate { get; set; }

        /// <summary>
        /// The value of each installment
        /// </summary>
        [JsonProperty("installmentsValue")]
        public int InstallmentsValue { get; set; }

        /// <summary>
        /// A hash that represents the device used to initiate the payment
        /// </summary>
        [JsonProperty("deviceFingerprint")]
        public string DeviceFingerprint { get; set; }

        /// <summary>
        /// The IP Address of the buyer
        /// </summary>
        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }

        /// <summary>
        /// Card
        /// </summary>
        [JsonProperty("card")]
        public VtexCard Card { get; set; }

        /// <summary>
        /// Cart
        /// </summary>
        [JsonProperty("miniCart")]
        public MiniCart MiniCart { get; set; }

        /// <summary>
        /// The order URL from merchant's backoffice
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// The URL you need to call to send the callbacks/notifications of payment status changes
        /// </summary>
        [JsonProperty("callbackUrl")]
        public string CallbackUrl { get; set; }

        /// <summary>
        /// The URL you need to redirect the end user back to merchant's store when using the redirect flow
        /// </summary>
        [JsonProperty("returnUrl")]
        public string ReturnUrl { get; set; }
    }

    public class VtexCardExpiration
    {
        /// <summary>
        /// Card expiration month (2-digits)
        /// </summary>
        [JsonProperty("month")]
        public string Month { get; set; }

        /// <summary>
        /// Card expiration year (4-digits)
        /// </summary>
        [JsonProperty("year")]
        public string Year { get; set; }
    }

    public class VtexCard
    {
        /// <summary>
        /// Card holder name
        /// </summary>
        [JsonProperty("holder")]
        public string Holder { get; set; }

        /// <summary>
        /// Card number
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>
        /// Card security code
        /// </summary>
        [JsonProperty("csc")]
        public string Csc { get; set; }

        /// <summary>
        /// Card expiration
        /// </summary>
        [JsonProperty("expiration")]
        public VtexCardExpiration Expiration { get; set; }
    }

    public class Buyer
    {
        /// <summary>
        /// Buyer's unique identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Buyer's first name
        /// </summary>
        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Buyer's last name
        /// </summary>
        [JsonProperty("lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Buyer's document number
        /// </summary>
        [JsonProperty("document")]
        public string Document { get; set; }

        /// <summary>
        /// Buyer's document type
        /// </summary>
        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        /// <summary>
        /// Buyer's email
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Buyer's phone number
        /// </summary>
        [JsonProperty("phone")]
        public string Phone { get; set; }
    }

    public class VtexShippingAddress
    {
        /// <summary>
        /// Shipping address: country
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// Shipping address: street
        /// </summary>
        [JsonProperty("street")]
        public string Street { get; set; }

        /// <summary>
        /// Shipping address: number
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>
        /// Shipping address: complement
        /// </summary>
        [JsonProperty("complement")]
        public string Complement { get; set; }

        /// <summary>
        /// Shipping address: neighborhood
        /// </summary>
        [JsonProperty("neighborhood")]
        public string Neighborhood { get; set; }

        /// <summary>
        /// Shipping address: postal code
        /// </summary>
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Shipping address: city
        /// </summary>
        [JsonProperty("city")]
        public string City { get; set; }

        /// <summary>
        /// Shipping address: state/province
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }
    }

    public class VtexBillingAddress
    {
        /// <summary>
        /// Billing address: country
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// Billing address: street
        /// </summary>
        [JsonProperty("street")]
        public string Street { get; set; }

        /// <summary>
        /// Billing address: number
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }

        /// <summary>
        /// Billing address: complement
        /// </summary>
        [JsonProperty("complement")]
        public string Complement { get; set; }

        /// <summary>
        /// Billing address: neighborhood
        /// </summary>
        [JsonProperty("neighborhood")]
        public string Neighborhood { get; set; }

        /// <summary>
        /// Billing address: postal code
        /// </summary>
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Billing address: city
        /// </summary>
        [JsonProperty("city")]
        public string City { get; set; }

        /// <summary>
        /// Billing address: state/province
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }
    }

    public class VtexItem
    {
        /// <summary>
        /// Item identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Item name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Item price per unit
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Item quantity
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Discount received for the item
        /// </summary>
        [JsonProperty("discount")]
        public decimal Discount { get; set; }
    }

    public class MiniCart
    {
        /// <summary>
        /// Total shipping value
        /// </summary>
        [JsonProperty("shippingValue")]
        public decimal ShippingValue { get; set; }

        /// <summary>
        /// Total tax value
        /// </summary>
        [JsonProperty("taxValue")]
        public decimal TaxValue { get; set; }

        /// <summary>
        /// Buyer infromation
        /// </summary>
        [JsonProperty("buyer")]
        public Buyer Buyer { get; set; }

        /// <summary>
        /// Shipping address
        /// </summary>
        [JsonProperty("shippingAddress")]
        public VtexShippingAddress ShippingAddress { get; set; }

        /// <summary>
        /// Billing address
        /// </summary>
        [JsonProperty("billingAddress")]
        public VtexBillingAddress BillingAddress { get; set; }

        /// <summary>
        /// Items
        /// </summary>
        [JsonProperty("items")]
        public List<VtexItem> Items { get; set; }
    }
}
