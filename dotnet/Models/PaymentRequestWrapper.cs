using Cybersource.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class PaymentRequestWrapper : CreatePaymentRequest
    {
        public string MerchantId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyTaxId { get; set; }
        public string CustomerName { get { return $"{ this.MiniCart.Buyer.FirstName } { this.MiniCart.Buyer.LastName }"; } }

        public PaymentRequestWrapper(CreatePaymentRequest createPaymentRequest)
        {
            this.Currency = createPaymentRequest.Currency;
            this.Card = new VtexCard{ Bin = createPaymentRequest.Card.Bin };
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
    }
}
