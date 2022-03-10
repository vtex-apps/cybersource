using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cybersource.Models
{
    /// <summary>
    /// This wrapper makes certain fields available for the merchant to add in Merchant Defined Data
    /// </summary>
    public class PaymentRequestWrapper : CreatePaymentRequest
    {
        public string MerchantId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyTaxId { get; set; }
        //public string CustomerName { get { return $"{ this.MiniCart.Buyer.FirstName } { this.MiniCart.Buyer.LastName }"; } }
        //public string Bin { get { return this.Card.Bin; } }

        public PaymentRequestWrapper(CreatePaymentRequest createPaymentRequest)
        {
            this.Currency = createPaymentRequest.Currency;
            this.Card = new VtexCard{ Bin = createPaymentRequest?.Card?.Bin };
            this.DeviceFingerprint = createPaymentRequest.DeviceFingerprint;
            this.Installments = createPaymentRequest.Installments;
            this.InstallmentsInterestRate = createPaymentRequest.InstallmentsInterestRate;
            this.InstallmentsValue = createPaymentRequest.InstallmentsValue;
            this.IpAddress = createPaymentRequest.IpAddress;
            this.MerchantName = createPaymentRequest.MerchantName;
            this.OrderId = createPaymentRequest.OrderId;
            this.PaymentId = createPaymentRequest.PaymentId;
            this.PaymentMethod = createPaymentRequest.PaymentMethod;
            this.Reference = createPaymentRequest.Reference;
            this.ShopperInteraction = createPaymentRequest.ShopperInteraction;
            this.TotalCartValue = createPaymentRequest.TotalCartValue;
            this.TransactionId = createPaymentRequest.TransactionId;
            this.Value = createPaymentRequest.Value;
            this.MiniCart = createPaymentRequest.MiniCart;
        }

        public List<string> ListProperties()
        {
            List<string> list = new List<string>();
            list.AddRange(this.ListProperties(new PaymentRequestWrapper(new CreatePaymentRequest()), null));
            list.AddRange(this.ListProperties(new VtexCard(), "Card"));
            list.AddRange(this.ListProperties(new MiniCart(), "MiniCart"));
            list.AddRange(this.ListProperties(new VtexBillingAddress(), "MiniCart.BillingAddress"));
            list.AddRange(this.ListProperties(new Buyer(), "MiniCart.Buyer"));
            list.AddRange(this.ListProperties(new VtexItem(), "MiniCart.Items[]"));
            list.AddRange(this.ListProperties(new VtexShippingAddress(), "MiniCart.ShippingAddress"));
            //list.AddRange(this.ListProperties(new MiniCart().ShippingValue, "MiniCart.ShippingValue"));
            //list.AddRange(this.ListProperties(new MiniCart().TaxRate, "MiniCart.TaxRate"));
            //list.AddRange(this.ListProperties(new MiniCart().TaxValue, "MiniCart.TaxValue"));
            return list;
        }

        public List<string> ListProperties(object obj, string stem)
        {
            if(!string.IsNullOrEmpty(stem))
            {
                stem += ".";
            }

            List<string> list = new List<string>();
            Type type = obj.GetType();
            PropertyInfo[] props = type.GetProperties();
            foreach (var prop in props)
            {
                list.Add($"{stem}{prop.Name}");
            }

            return list;
        }

        public List<string> GetPropertyList()
        {
            return new List<string>
            {
                "MerchantId",
                "CompanyName",
                "CompanyTaxId",
                "Reference",
                "OrderId",
                "ShopperInteraction",
                "TransactionId",
                "PaymentId",
                "PaymentMethod",
                "MerchantName",
                "Value",
                "Currency",
                "Installments",
                "InstallmentsInterestRate",
                "InstallmentsValue",
                "DeviceFingerprint",
                "IpAddress",
                "TotalCartValue",
                "Card.Bin",
                "MiniCart.ShippingValue",
                "MiniCart.TaxValue",
                "MiniCart.TaxRate",
                "MiniCart.Buyer",
                "MiniCart.ShippingAddress",
                "MiniCart.BillingAddress",
                "MiniCart.Items",
                "MiniCart.BillingAddress.Country",
                "MiniCart.BillingAddress.Street",
                "MiniCart.BillingAddress.Number",
                "MiniCart.BillingAddress.Complement",
                "MiniCart.BillingAddress.Neighborhood",
                "MiniCart.BillingAddress.PostalCode",
                "MiniCart.BillingAddress.City",
                "MiniCart.BillingAddress.State",
                "MiniCart.Buyer.Id",
                "MiniCart.Buyer.FirstName",
                "MiniCart.Buyer.LastName",
                "MiniCart.Buyer.Document",
                "MiniCart.Buyer.DocumentType",
                "MiniCart.Buyer.Email",
                "MiniCart.Buyer.Phone",
                "MiniCart.Buyer.CorporateName",
                "MiniCart.Buyer.TradeName",
                "MiniCart.Buyer.CorporateDocument",
                "MiniCart.Buyer.IsCorporate",
                "MiniCart.Items[].Id",
                "MiniCart.Items[].Name",
                "MiniCart.Items[].Price",
                "MiniCart.Items[].Quantity",
                "MiniCart.Items[].Discount",
                "MiniCart.Items[].DeliveryType",
                "MiniCart.Items[].CategoryId",
                "MiniCart.Items[].SellerId",
                "MiniCart.Items[].TaxValue",
                "MiniCart.Items[].TaxRate",
                "MiniCart.ShippingAddress.Country",
                "MiniCart.ShippingAddress.Street",
                "MiniCart.ShippingAddress.Number",
                "MiniCart.ShippingAddress.Complement",
                "MiniCart.ShippingAddress.Neighborhood",
                "MiniCart.ShippingAddress.PostalCode",
                "MiniCart.ShippingAddress.City",
                "MiniCart.ShippingAddress.State"
            };
        }
    }
}
