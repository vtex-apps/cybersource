using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        public CustomDataWrapper CustomData { get; private set; }
        public MarketingDataWrapper MarketingData { get; set; }
        public ContextData ContextData { get; set; }


        public string FlattenCustomData(CustomData customData)
        {
            string retval = null;
            try
            {
                this.CustomData = new CustomDataWrapper();
                this.CustomData.CustomApps = new ExpandoObject();
                foreach (CustomApp customApp in customData.CustomApps)
                {
                    string propName = customApp.Id;
                    JObject fieldsArray = (JObject)customApp.Fields;
                    Dictionary<string, string> fields = fieldsArray.ToObject<Dictionary<string, string>>();
                    foreach (string fieldName in fields.Keys)
                    {
                        string fieldValue = fields[fieldName];
                        ((IDictionary<string, object>)this.CustomData.CustomApps).Add($"{propName}_{fieldName}", fieldValue);
                        //Console.WriteLine($"'{propName}_{fieldName}' = '{fieldValue}'");
                    }
                }
            }
            catch (Exception ex)
            {
                retval = $"ERROR FLATTENING DATA {ex.Message}";
            }

            return retval;
        }

        public string SetMarketingData(MarketingData marketingData)
        {
            string retval = null;
            try
            {
                this.MarketingData = new MarketingDataWrapper
                {
                    Coupon = marketingData.Coupon,
                    Id = marketingData.Id,
                    UtmCampaign = marketingData.UtmCampaign,
                    UtmiCampaign = marketingData.UtmiCampaign,
                    Utmipage = marketingData.Utmipage,
                    UtmiPart = marketingData.UtmiPart,
                    UtmMedium = marketingData.UtmMedium,
                    UtmPartner = marketingData.UtmPartner,
                    UtmSource = marketingData.UtmSource
                };

                this.MarketingData.MarketingTags = string.Join(',', marketingData.MarketingTags);
            }
            catch (Exception ex)
            {
                retval = $"ERROR Adding Marketing Data {ex.Message}";
            }

            return retval;
        }

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

        public PaymentRequestWrapper(SendAntifraudDataRequest sendAntifraudDataRequest)
        {
            this.Currency = sendAntifraudDataRequest.Payments[0].CurrencyIso4217;
            this.Card = new VtexCard { Bin = sendAntifraudDataRequest.Payments[0].Details.Bin };
            this.DeviceFingerprint = sendAntifraudDataRequest.DeviceFingerprint;
            this.Installments = (int)sendAntifraudDataRequest.Payments[0].Installments;
            this.IpAddress = sendAntifraudDataRequest.Ip;
            this.PaymentMethod = sendAntifraudDataRequest.Payments[0].Method;
            this.Reference = sendAntifraudDataRequest.Reference;
            this.TransactionId = sendAntifraudDataRequest.Id;
            this.Value = sendAntifraudDataRequest.Value;
            this.MiniCart = new MiniCart
            {
                Buyer = new Buyer
                {
                    Document = sendAntifraudDataRequest.MiniCart.Buyer.Document,
                    Email = sendAntifraudDataRequest.MiniCart.Buyer.Email,
                    DocumentType = sendAntifraudDataRequest.MiniCart.Buyer.DocumentType,
                    FirstName = sendAntifraudDataRequest.MiniCart.Buyer.FirstName,
                    Id = sendAntifraudDataRequest.MiniCart.Buyer.Id,
                    LastName = sendAntifraudDataRequest.MiniCart.Buyer.LastName,
                    Phone = sendAntifraudDataRequest.MiniCart.Buyer.Phone,
                },
                BillingAddress = new VtexBillingAddress
                {
                    City = sendAntifraudDataRequest.MiniCart.Buyer.Address.City,
                    Complement = sendAntifraudDataRequest.MiniCart.Buyer.Address.Complement,
                    Country = sendAntifraudDataRequest.MiniCart.Buyer.Address.Complement,
                    Neighborhood = sendAntifraudDataRequest.MiniCart.Buyer.Address.Neighborhood,
                    Number = sendAntifraudDataRequest.MiniCart.Buyer.Address.Number,
                    PostalCode = sendAntifraudDataRequest.MiniCart.Buyer.Address.PostalCode,
                    State = sendAntifraudDataRequest.MiniCart.Buyer.Address.State,
                    Street = sendAntifraudDataRequest.MiniCart.Buyer.Address.Street
                },
                ShippingAddress = new VtexShippingAddress
                {
                    City = sendAntifraudDataRequest.MiniCart.Shipping.Address.City,
                    Complement = sendAntifraudDataRequest.MiniCart.Shipping.Address.Complement,
                    Country = sendAntifraudDataRequest.MiniCart.Shipping.Address.Country,
                    Neighborhood = sendAntifraudDataRequest.MiniCart.Shipping.Address.Neighborhood,
                    Number = sendAntifraudDataRequest.MiniCart.Shipping.Address.Number,
                    PostalCode = sendAntifraudDataRequest.MiniCart.Shipping.Address.PostalCode,
                    State = sendAntifraudDataRequest.MiniCart.Shipping.Address.State,
                    Street = sendAntifraudDataRequest.MiniCart.Shipping.Address.Street
                },
                Items = new List<VtexItem>(),
                ShippingValue = sendAntifraudDataRequest.MiniCart.Shipping.Value,
                TaxValue = sendAntifraudDataRequest.MiniCart.TaxValue
            };

            this.TotalCartValue = (double)sendAntifraudDataRequest.Value;
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
                //"MiniCart.Items[].Id",
                //"MiniCart.Items[].Name",
                //"MiniCart.Items[].Price",
                //"MiniCart.Items[].Quantity",
                //"MiniCart.Items[].Discount",
                //"MiniCart.Items[].DeliveryType",
                //"MiniCart.Items[].CategoryId",
                //"MiniCart.Items[].SellerId",
                //"MiniCart.Items[].TaxValue",
                //"MiniCart.Items[].TaxRate",
                "MiniCart.ShippingAddress.Country",
                "MiniCart.ShippingAddress.Street",
                "MiniCart.ShippingAddress.Number",
                "MiniCart.ShippingAddress.Complement",
                "MiniCart.ShippingAddress.Neighborhood",
                "MiniCart.ShippingAddress.PostalCode",
                "MiniCart.ShippingAddress.City",
                "MiniCart.ShippingAddress.State",
                "MarketingData.Id",
                "MarketingData.UtmSource",
                "MarketingData.UtmPartner",
                "MarketingData.UtmMedium",
                "MarketingData.UtmCampaign",
                "MarketingData.Coupon",
                "MarketingData.UtmiCampaign",
                "MarketingData.Utmipage",
                "MarketingData.UtmiPart",
                "MarketingData.MarketingTags",
                "ContextData.LoggedIn"
            };
        }
    }

    public class CustomDataWrapper
    {
        public dynamic CustomApps { get; set; }
        //public Dictionary<string, string> CustomApps { get; set; }
    }

    public class MarketingDataWrapper : MarketingData
    {
        public new string MarketingTags { get; set; }
    }

    public class ContextDataWrapper
    {
        public bool? LoggedIn { get; set; }
    }
}
