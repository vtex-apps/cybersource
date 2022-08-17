using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cybersource.Data;
using Cybersource.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vtex.Api.Context;

namespace Cybersource.Services
{
    public class CybersourcePaymentService : ICybersourcePaymentService
    {
        private readonly IIOServiceContext _context;
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ICybersourceApi _cybersourceApi;
        private readonly ICybersourceRepository _cybersourceRepository;
        private readonly IVtexApiService _vtexApiService;

        public CybersourcePaymentService(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ICybersourceApi cybersourceApi, ICybersourceRepository cybersourceRepository, IVtexApiService vtexApiService)
        {
            this._context = context ??
                            throw new ArgumentNullException(nameof(context));

            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._cybersourceApi = cybersourceApi ??
                               throw new ArgumentNullException(nameof(cybersourceApi));

            this._cybersourceRepository = cybersourceRepository ??
                               throw new ArgumentNullException(nameof(cybersourceRepository));

            this._vtexApiService = vtexApiService ??
                               throw new ArgumentNullException(nameof(vtexApiService));
        }

        #region Payments
        public async Task<Payments> BuildPayment(CreatePaymentRequest createPaymentRequest)
        {
            Payments payment = null;
            try
            {
                _context.Vtex.Logger.Debug("BuildPayment", null, "Creating Payment", new[] { ("orderId", createPaymentRequest.OrderId), ("paymentId", createPaymentRequest.PaymentId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)) });
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                string merchantName = createPaymentRequest.MerchantName;
                string merchantTaxId = string.Empty;
                bool doCapture = false;
                if (createPaymentRequest.MerchantSettings != null)
                {
                    foreach (MerchantSetting merchantSetting in createPaymentRequest.MerchantSettings)
                    {
                        switch (merchantSetting.Name)
                        {
                            case CybersourceConstants.ManifestCustomField.CompanyName:
                                merchantName = merchantSetting.Value;
                                break;
                            case CybersourceConstants.ManifestCustomField.CompanyTaxId:
                                merchantTaxId = merchantSetting.Value;
                                break;
                            case CybersourceConstants.ManifestCustomField.CaptureSetting:
                                if (merchantSetting.Value != null)
                                {
                                    doCapture = merchantSetting.Value.Equals(CybersourceConstants.CaptureSetting.ImmediateCapture);
                                }

                                break;

                            case CybersourceConstants.ManifestCustomField.MerchantId:
                                if (!string.IsNullOrWhiteSpace(merchantSettings.MerchantId))
                                {
                                    merchantSettings.MerchantId = merchantSetting.Value;
                                }

                                break;
                            case CybersourceConstants.ManifestCustomField.MerchantKey:
                                if (!string.IsNullOrWhiteSpace(merchantSettings.MerchantKey))
                                {
                                    merchantSettings.MerchantKey = merchantSetting.Value;
                                }

                                break;
                            case CybersourceConstants.ManifestCustomField.SharedSecretKey:
                                if (!string.IsNullOrWhiteSpace(merchantSettings.SharedSecretKey))
                                {
                                    merchantSettings.SharedSecretKey = merchantSetting.Value;
                                }

                                break;
                        }
                    }
                }

                this.BinLookup(createPaymentRequest.Card.Bin, createPaymentRequest.PaymentMethod, out bool isDebit, out string cardType, out CybersourceConstants.CardType cardBrandName);

                payment = new Payments
                {
                    merchantInformation = new MerchantInformation
                    {
                        merchantName = merchantName,
                        taxId = merchantTaxId
                    },
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = createPaymentRequest.OrderId, // Use Reference to have a consistent number with Fraud?
                        //transactionId = createPaymentRequest.TransactionId,
                        applicationName = $"{_context.Vtex.App.Vendor}.{_context.Vtex.App.Name}",
                        applicationVersion = _context.Vtex.App.Version,
                        applicationUser = _context.Vtex.Account,
                        partner = new Partner
                        {
                            solutionId = CybersourceConstants.SOLUTION_ID
                        }
                    },
                    paymentInformation = new PaymentInformation
                    {
                        card = new Card
                        {
                            number = createPaymentRequest.Card.NumberToken ?? createPaymentRequest.Card.Number,
                            securityCode = createPaymentRequest.Card.CscToken ?? createPaymentRequest.Card.Csc,
                            expirationMonth = createPaymentRequest.Card.Expiration.Month,
                            expirationYear = createPaymentRequest.Card.Expiration.Year,
                            type = cardType
                        }
                    },
                    orderInformation = new OrderInformation
                    {
                        amountDetails = new AmountDetails
                        {
                            totalAmount = createPaymentRequest.Value.ToString(),
                            currency = createPaymentRequest.Currency,
                            taxAmount = createPaymentRequest.MiniCart.TaxValue.ToString(),
                            freightAmount = createPaymentRequest.MiniCart.ShippingValue.ToString()
                        },
                        billTo = new BillTo
                        {
                            firstName = createPaymentRequest.MiniCart.Buyer.FirstName,
                            lastName = createPaymentRequest.MiniCart.Buyer.LastName,
                            address1 = $"{createPaymentRequest.MiniCart.BillingAddress.Number} {createPaymentRequest.MiniCart.BillingAddress.Street}",
                            address2 = createPaymentRequest.MiniCart.BillingAddress.Complement,
                            locality = createPaymentRequest.MiniCart.BillingAddress.City ?? createPaymentRequest.MiniCart.BillingAddress.Neighborhood,
                            administrativeArea = GetAdministrativeArea(createPaymentRequest.MiniCart.BillingAddress.State, this.GetCountryCode(createPaymentRequest.MiniCart.BillingAddress.Country)),
                            postalCode = createPaymentRequest.MiniCart.BillingAddress.PostalCode,
                            country = this.GetCountryCode(createPaymentRequest.MiniCart.BillingAddress.Country),
                            district = createPaymentRequest.MiniCart.BillingAddress.Neighborhood,
                            email = createPaymentRequest.MiniCart.Buyer.Email,
                            phoneNumber = createPaymentRequest.MiniCart.Buyer.Phone
                        },
                        shipTo = new ShipTo
                        {
                            address1 = $"{createPaymentRequest.MiniCart.ShippingAddress.Number} {createPaymentRequest.MiniCart.ShippingAddress.Street}",
                            address2 = createPaymentRequest.MiniCart.ShippingAddress.Complement,
                            administrativeArea = GetAdministrativeArea(createPaymentRequest.MiniCart.ShippingAddress.State, this.GetCountryCode(createPaymentRequest.MiniCart.ShippingAddress.Country)),
                            country = this.GetCountryCode(createPaymentRequest.MiniCart.ShippingAddress.Country),
                            postalCode = createPaymentRequest.MiniCart.ShippingAddress.PostalCode,
                            locality = createPaymentRequest.MiniCart.ShippingAddress.City ?? createPaymentRequest.MiniCart.ShippingAddress.Neighborhood,
                            district = createPaymentRequest.MiniCart.ShippingAddress.Neighborhood,
                            phoneNumber = createPaymentRequest.MiniCart.Buyer.Phone, // Note that this is the buyer's number, we do not have a number for the shipping destination
                            firstName = createPaymentRequest.MiniCart.Buyer.FirstName, // defaulting to buyer info.  This should be ovverridden from the order data
                            lastName = createPaymentRequest.MiniCart.Buyer.LastName,
                        },
                        lineItems = new List<LineItem>()
                    },
                    deviceInformation = new DeviceInformation
                    {
                        ipAddress = createPaymentRequest.IpAddress,
                        fingerprintSessionId = createPaymentRequest.DeviceFingerprint
                    },
                    buyerInformation = new BuyerInformation
                    {
                        personalIdentification = new List<PersonalIdentification>
                        {
                            new PersonalIdentification
                            {
                                id = createPaymentRequest.MiniCart.Buyer.Document,
                                type = createPaymentRequest.MiniCart.Buyer.DocumentType
                            }
                        }
                    }
                };

                // Make a copy of the payment request and add fields that are not available in the payment request / skip fields that should not be exposed
                PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(createPaymentRequest);
                requestWrapper.MerchantId = merchantSettings.MerchantId;
                requestWrapper.CompanyName = merchantName;
                requestWrapper.CompanyTaxId = merchantTaxId;

                payment.processingInformation = new ProcessingInformation();
                if (doCapture)
                {
                    payment.processingInformation.capture = "true";
                }

                string numberOfInstallments = createPaymentRequest.Installments.ToString("00");
                string plan = string.Empty;
                decimal installmentsInterestRate = createPaymentRequest.InstallmentsInterestRate;

                switch (merchantSettings.Processor)
                {
                    case CybersourceConstants.Processors.Braspag:
                        if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Colombia))
                        {
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalCount = numberOfInstallments
                            };
                        }
                        else if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Mexico))
                        {
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                            payment.processingInformation.reconciliationId = createPaymentRequest.PaymentId;
                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalCount = numberOfInstallments
                            };
                        }
                        else
                        {
                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalAmount = createPaymentRequest.InstallmentsValue.ToString()
                            };
                        }

                        break;
                    case CybersourceConstants.Processors.VPC:
                        if (
                            merchantSettings.Region.Equals(CybersourceConstants.Regions.Colombia) &&
                            CybersourceConstants.CardType.Unknown.Equals(CybersourceConstants.CardType.Visa) &&
                            !isDebit
                        )
                        {
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalCount = numberOfInstallments
                            };
                        }

                        break;
                    case CybersourceConstants.Processors.Izipay:
                        if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Peru))
                        {
                            if (CybersourceConstants.CardType.Unknown.Equals(CybersourceConstants.CardType.Visa) || CybersourceConstants.CardType.Unknown.Equals(CybersourceConstants.CardType.MasterCard))
                            {
                                plan = "0";  // 0: no deferred payment, 1: 30 días, 2: 60 días, 3: 90 días
                                payment.issuerInformation = new IssuerInformation
                                {
                                    //POS 1 - 6:014001
                                    //POS 7 - 8: # of installments
                                    //POS 9 - 16:00000000
                                    //POS 17: plan(0: no deferred payment, 1: 30 días, 2: 60 días, 3: 90 días)
                                    discretionaryData = $"14001{numberOfInstallments}00000000{plan}"
                                };
                            }
                        }


                        break;
                    case CybersourceConstants.Processors.eGlobal:
                    case CybersourceConstants.Processors.BBVA:
                        if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Mexico))
                        {
                            plan = installmentsInterestRate > 0 ? "05" : "03";  // 03 no interest 05 with interest
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT_INTERNET;
                            //POS 1 - 2: # of months of deferred payment
                            //POS 3 - 4: # installments
                            //POS 5 - 6: plan(03 no interest, 05 with interest)"
                            string monthsDeferred = "00";
                            payment.installmentInformation = new InstallmentInformation
                            {
                                amount = $"{monthsDeferred}{numberOfInstallments}{plan}",
                                totalCount = numberOfInstallments
                            };
                        }

                        break;
                    case CybersourceConstants.Processors.Banorte:
                    case CybersourceConstants.Processors.AmexDirect:
                        payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                        payment.processingInformation.reconciliationId = createPaymentRequest.PaymentId;
                        payment.installmentInformation = new InstallmentInformation
                        {
                            totalCount = numberOfInstallments
                        };

                        break;
                    case CybersourceConstants.Processors.Prosa:
                    case CybersourceConstants.Processors.Santander:
                        //Where
                        //commerceIndicator = ""internet"" in case there is no installments and no installment information shall be sent.
                        //If there are installments, then:
                        //-planType: 00: Not a promotion; 03: No interest, 05: with interest, 07: buy now and pay all later
                        //-totalCount: # installments
                        //-gracePeriodDuration: if planType = 07 and totalCount = 00, this must be greater than 00
                        plan = installmentsInterestRate > 0 ? "05" : "03";
                        payment.processingInformation.commerceIndicator = createPaymentRequest.Installments > 1 ? CybersourceConstants.INSTALLMENT : CybersourceConstants.INTERNET;
                        payment.processingInformation.reconciliationId = createPaymentRequest.PaymentId;
                        payment.installmentInformation = new InstallmentInformation
                        {
                            totalCount = numberOfInstallments,
                            planType = plan,
                            gracePeriodDuration = "12"
                        };

                        break;
                    default:
                        payment.installmentInformation = new InstallmentInformation
                        {
                            totalAmount = createPaymentRequest.InstallmentsValue.ToString(),
                            totalCount = numberOfInstallments
                        };

                        break;
                }

                VtexOrder[] vtexOrders = await _vtexApiService.GetOrderGroup(createPaymentRequest.OrderId);
                List<VtexOrderItem> vtexOrderItems = new List<VtexOrderItem>();
                if (vtexOrders != null)
                {
                    foreach (VtexOrder vtexOrder in vtexOrders)
                    {
                        // ContextData is not returned in the order group list
                        if (merchantSettings.MerchantDefinedValueSettings.Any(ms => ms.UserInput.Contains("ContextData")) || merchantSettings.MerchantDefinedValueSettings.Any(ms => ms.UserInput.Contains("PersonalData")))
                        {
                            VtexOrder vtexCheckoutOrder = await _vtexApiService.GetOrderInformation(vtexOrder.OrderId);
                            requestWrapper.ContextData = new ContextData
                            {
                                LoggedIn = vtexCheckoutOrder?.ContextData?.LoggedIn ?? false,
                                HasAccessToOrderFormEnabledByLicenseManager = vtexCheckoutOrder?.ContextData?.HasAccessToOrderFormEnabledByLicenseManager,
                                UserAgent = vtexCheckoutOrder?.ContextData?.UserAgent,
                                UserId = vtexCheckoutOrder?.ContextData?.UserId
                            };

                            PersonalData personalData = await _vtexApiService.GetPersonalData(vtexCheckoutOrder.UserProfileId);
                            if (personalData != null)
                            {
                                requestWrapper.PersonalData = new PersonalData
                                {
                                    BirthDate = personalData.BirthDate,
                                    BusinessDocument = personalData.BusinessDocument,
                                    BusinessPhone = personalData.BusinessPhone,
                                    CellPhone = personalData.CellPhone,
                                    CorporateName = personalData.CorporateName,
                                    CreatedIn = personalData.CreatedIn,
                                    CustomerClass = personalData.CustomerClass,
                                    Document = personalData.Document,
                                    DocumentType = personalData.DocumentType,
                                    Email = personalData.Email,
                                    FancyName = personalData.FancyName,
                                    FirstName = personalData.FirstName,
                                    Gender = personalData.Gender,
                                    HomePhone = personalData.HomePhone,
                                    IsFreeStateRegistration = personalData.IsFreeStateRegistration,
                                    IsPj = personalData.IsPj,
                                    LastName = personalData.LastName,
                                    NickName = personalData.NickName,
                                    StateRegistration = personalData.StateRegistration,
                                    UserId = personalData.UserId
                                };
                            }
                        }

                        foreach (VtexOrderItem vtexItem in vtexOrder.Items)
                        {
                            if (!vtexOrderItems.Contains(vtexItem))
                            {
                                vtexOrderItems.Add(vtexItem);
                            }
                        }

                        if (vtexOrder.CustomData != null && vtexOrder.CustomData.CustomApps != null)
                        {
                            string response = requestWrapper.FlattenCustomData(vtexOrder.CustomData);
                            if (!string.IsNullOrEmpty(response))
                            {
                                // A response indicates an error.
                                _context.Vtex.Logger.Error("BuildPayment", "FlattenCustomData", response, null, new[] { ("vtexOrder.CustomData", JsonConvert.SerializeObject(vtexOrder.CustomData)), ("requestWrapper", JsonConvert.SerializeObject(requestWrapper)) });
                            }
                        }

                        if (vtexOrder.MarketingData != null)
                        {
                            string response = requestWrapper.SetMarketingData(vtexOrder.MarketingData);
                            if (!string.IsNullOrEmpty(response))
                            {
                                // A response indicates an error.
                                _context.Vtex.Logger.Error("BuildPayment", "SetMarketingData", response, null, new[] { ("vtexOrder.MarketingData", JsonConvert.SerializeObject(vtexOrder.MarketingData)), ("requestWrapper", JsonConvert.SerializeObject(requestWrapper)) });
                            }
                        }

                        if (!string.IsNullOrEmpty(vtexOrder.ShippingData.Address.ReceiverName) && vtexOrder.ShippingData.Address.ReceiverName.Trim().Length > 2) // Check that the Receiver Name is at least 3 characters
                        {
                            string[] nameArr = vtexOrder.ShippingData.Address.ReceiverName.Split(' ', 2);
                            if (nameArr.Length <= 1)
                            {
                                payment.orderInformation.shipTo.firstName = string.Empty;
                                payment.orderInformation.shipTo.lastName = vtexOrder.ShippingData.Address.ReceiverName;
                            }
                            else
                            {
                                payment.orderInformation.shipTo.firstName = nameArr[0];
                                payment.orderInformation.shipTo.lastName = nameArr[1];
                            }
                        }

                        if (merchantSettings.MerchantDefinedValueSettings.Any(ms => ms.UserInput.Contains("ClientProfileData")))
                        {
                            requestWrapper.ClientProfileData = new ClientProfileData
                            {
                                CorporateDocument = vtexOrder.ClientProfileData.CorporateDocument,
                                CorporateName = vtexOrder.ClientProfileData.CorporateName,
                                CorporatePhone = vtexOrder.ClientProfileData.CorporatePhone,
                                CustomerClass = vtexOrder.ClientProfileData.CustomerClass,
                                Document = vtexOrder.ClientProfileData.Document,
                                DocumentType = vtexOrder.ClientProfileData.DocumentType,
                                Email = vtexOrder.ClientProfileData.Email,
                                FirstName = vtexOrder.ClientProfileData.FirstName,
                                IsCorporate = vtexOrder.ClientProfileData.IsCorporate,
                                LastName = vtexOrder.ClientProfileData.LastName,
                                Phone = vtexOrder.ClientProfileData.Phone,
                                StateInscription = vtexOrder.ClientProfileData.StateInscription,
                                TradeName = vtexOrder.ClientProfileData.TradeName
                            };
                        }

                        if (merchantSettings.MerchantDefinedValueSettings.Any(ms => ms.UserInput.Contains("Shipping")))
                        {
                            LogisticsInfo logisticsInfo = vtexOrder.ShippingData.LogisticsInfo.FirstOrDefault();
                            Sla selectedSla = logisticsInfo.Slas.FirstOrDefault(s => s.Id.Equals(logisticsInfo.SelectedSla, StringComparison.InvariantCultureIgnoreCase));
                            requestWrapper.Shipping = new SlaWrapper
                            {
                                AvailableDeliveryWindows = selectedSla.AvailableDeliveryWindows,
                                DeliveryChannel = selectedSla.DeliveryChannel,
                                DeliveryIds = new List<DeliveryId>(),
                                DeliveryWindow = selectedSla.DeliveryWindow,
                                Id = selectedSla.Id,
                                ListPrice = selectedSla.ListPrice,
                                LockTtl = selectedSla.LockTtl,
                                Name = selectedSla.Name,
                                PickupDistance = selectedSla.PickupDistance,
                                PickupPointId = selectedSla.PickupPointId,
                                PickupStoreInfo = new PickupStoreInfo
                                {
                                    AdditionalInfo = selectedSla.PickupStoreInfo.AdditionalInfo,
                                    Address = selectedSla.PickupStoreInfo.Address != null ? new VtexAddress
                                    {
                                        AddressId = selectedSla.PickupStoreInfo.Address.AddressId,
                                        AddressType = selectedSla.PickupStoreInfo.Address.AddressType,
                                        City = selectedSla.PickupStoreInfo.Address.City,
                                        Complement = selectedSla.PickupStoreInfo.Address.Complement,
                                        Country = selectedSla.PickupStoreInfo.Address.Country,
                                        GeoCoordinates = selectedSla.PickupStoreInfo.Address.GeoCoordinates,
                                        IsDisposable = selectedSla.PickupStoreInfo.Address.IsDisposable,
                                        Neighborhood = selectedSla.PickupStoreInfo.Address.Neighborhood,
                                        Number = selectedSla.PickupStoreInfo.Address.Number,
                                        PostalCode = selectedSla.PickupStoreInfo.Address.PostalCode,
                                        ReceiverName = selectedSla.PickupStoreInfo.Address.ReceiverName,
                                        Reference = selectedSla.PickupStoreInfo.Address.Reference,
                                        State = selectedSla.PickupStoreInfo.Address.State,
                                        Street = selectedSla.PickupStoreInfo.Address.Street
                                    } : null,
                                    DockId = selectedSla.PickupStoreInfo.DockId,
                                    FriendlyName = selectedSla.PickupStoreInfo.FriendlyName,
                                    IsPickupStore = selectedSla.PickupStoreInfo.IsPickupStore
                                },
                                PolygonName = selectedSla.PolygonName,
                                ShippingEstimate = selectedSla.ShippingEstimate,
                                ShippingEstimateDate = selectedSla.ShippingEstimateDate,
                                Price = selectedSla.Price,
                                Tax = selectedSla.Tax,
                                TransitTime = selectedSla.TransitTime
                            };

                            requestWrapper.Shipping.DeliveryIds.AddRange(selectedSla.DeliveryIds);
                            requestWrapper.Shipping.CourierName = string.Join(", ", selectedSla.DeliveryIds.Select(d => d.CourierName).Distinct().ToArray());
                        }

                        if (requestWrapper.Totals == null)
                        {
                            requestWrapper.Totals = new Totals
                            {
                                Discounts = 0m,
                                Items = 0m,
                                Shipping = 0m,
                                Tax = 0m,
                            };
                        }

                        foreach (VtexTotal vtexTotal in vtexOrder.Totals)
                        {
                            switch (vtexTotal.Id)
                            {
                                case "Discounts":
                                    requestWrapper.Totals.Discounts += (decimal)vtexTotal.Value / 100;
                                    break;
                                case "Items":
                                    requestWrapper.Totals.Items += (decimal)vtexTotal.Value / 100;
                                    break;
                                case "Shipping":
                                    requestWrapper.Totals.Shipping += (decimal)vtexTotal.Value / 100;
                                    break;
                                case "Tax":
                                    requestWrapper.Totals.Tax += (decimal)vtexTotal.Value / 100;
                                    break;
                            }
                        }
                    }
                }

                foreach (VtexItem vtexItem in createPaymentRequest.MiniCart.Items)
                {
                    string taxAmount = null;
                    string commodityCode = null;
                    VtexOrderItem vtexOrderItem = vtexOrderItems.FirstOrDefault(i => i.Id.Equals(vtexItem.Id));
                    if (vtexOrderItem != null)
                    {
                        long itemTax = 0L;
                        foreach (PriceTag priceTag in vtexOrderItem.PriceTags)
                        {
                            string name = priceTag.Name.ToLower();
                            if (name.Contains("tax@") || name.Contains("taxhub@"))
                            {
                                if (priceTag.IsPercentual ?? false)
                                {
                                    itemTax += (long)Math.Round(vtexOrderItem.SellingPrice * priceTag.RawValue, MidpointRounding.AwayFromZero);
                                }
                                else
                                {
                                    itemTax += priceTag.Value / vtexOrderItem.Quantity;
                                }
                            }
                        }

                        taxAmount = ((decimal)itemTax / 100).ToString();
                        commodityCode = vtexOrderItem.TaxCode;
                    }

                    LineItem lineItem = new LineItem
                    {
                        productSKU = vtexItem.Id,
                        productName = vtexItem.Name,
                        unitPrice = (vtexItem.Price + (vtexItem.Discount / vtexItem.Quantity)).ToString(), // Discount is negative
                        quantity = vtexItem.Quantity.ToString(),
                        discountAmount = vtexItem.Discount.ToString(),
                        taxAmount = taxAmount,
                        commodityCode = commodityCode
                    };

                    payment.orderInformation.lineItems.Add(lineItem);
                }

                if (merchantSettings.MerchantDefinedValueSettings.Any(ms => ms.UserInput.Contains("AdditionalData")))
                {
                    // Get last order date and number of orders
                    VtexOrderList vtexOrderList = await _vtexApiService.ListOrders($"orderBy=creationDate,desc&q={createPaymentRequest.MiniCart.Buyer.Email}");
                    if (vtexOrderList != null && vtexOrderList.Paging != null && vtexOrderList.List != null)
                    {
                        try
                        {
                            if (requestWrapper.AdditionalData == null)
                            {
                                requestWrapper.AdditionalData = new AdditionalData();
                            }

                            requestWrapper.AdditionalData.NumberOfPreviousPurchases = vtexOrderList.Paging.Total;
                            requestWrapper.AdditionalData.DateOfLastPurchase = vtexOrderList.List.Select(o => o.CreationDate).FirstOrDefault();
                        }
                        catch (Exception ex)
                        {
                            _context.Vtex.Logger.Error("BuildPayment", "OrderList", "Error", ex);
                        }
                    }
                }

                // Set Merchant Defined Information fields
                payment.merchantDefinedInformation = await this.GetMerchantDefinedInformation(merchantSettings, requestWrapper);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("BuildPayment", "OrderList", "Error", ex);
            }

            return payment;
        }

        public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest)
        {
            CreatePaymentResponse createPaymentResponse = null;
            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(createPaymentRequest.PaymentId);
                if (paymentData != null && paymentData.CreatePaymentResponse != null)
                {
                    _context.Vtex.Logger.Debug("CreatePayment", null, "Loaded PaymentData", new[] { ("orderId", createPaymentRequest.OrderId), ("paymentId", createPaymentRequest.PaymentId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)), ("paymentData", JsonConvert.SerializeObject(paymentData)) });
                    await _vtexApiService.ProcessConversions();
                    return paymentData.CreatePaymentResponse;
                }

                Payments payment = await this.BuildPayment(createPaymentRequest);
                PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessPayment(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl);
                if (paymentsResponse != null)
                {
                    _context.Vtex.Logger.Debug("CreatePayment", "PaymentService", "Processing Payment", new[] { ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)), ("payment", JsonConvert.SerializeObject(payment)), ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse)) });
                    createPaymentResponse = new CreatePaymentResponse();
                    createPaymentResponse.AuthorizationId = paymentsResponse.Id;
                    createPaymentResponse.Tid = paymentsResponse.Id;
                    createPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message ?? paymentsResponse.Status;

                    var errorInformation = paymentsResponse.ErrorInformation != null
                        ? paymentsResponse.ErrorInformation.Reason
                        : paymentsResponse.Status;

                    createPaymentResponse.Code = paymentsResponse.ProcessorInformation != null
                        ? paymentsResponse.ProcessorInformation.ResponseCode
                        : errorInformation;

                    string paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
                    // AUTHORIZED
                    // PARTIAL_AUTHORIZED
                    // AUTHORIZED_PENDING_REVIEW
                    // AUTHORIZED_RISK_DECLINED
                    // PENDING_AUTHENTICATION
                    // PENDING_REVIEW
                    // DECLINED
                    // INVALID_REQUEST
                    switch (paymentsResponse.Status)
                    {
                        case "AUTHORIZED":
                        case "PARTIAL_AUTHORIZED":
                            // Check for pre-auth fraud status
                            SendAntifraudDataResponse antifraudDataResponse = await _cybersourceRepository.GetAntifraudData(createPaymentRequest.TransactionId);
                            if (antifraudDataResponse != null)
                            {
                                paymentStatus = antifraudDataResponse.Status;
                            }
                            else
                            {
                                paymentStatus = CybersourceConstants.VtexAuthStatus.Approved;
                            }

                            break;

                        case "AUTHORIZED_PENDING_REVIEW":
                        case "PENDING_AUTHENTICATION":
                        case "PENDING_REVIEW":
                        case "INVALID_REQUEST":
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
                            createPaymentResponse.DelayToCancel = 5 * 60 * 60 * 24;
                            break;
                        case "DECLINED":
                        case "AUTHORIZED_RISK_DECLINED":
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                            break;
                    }

                    createPaymentResponse.Status = paymentStatus;
                    if (paymentsResponse.ProcessorInformation != null)
                    {
                        createPaymentResponse.Nsu = paymentsResponse.ProcessorInformation.TransactionId;
                    }

                    createPaymentResponse.PaymentId = createPaymentRequest.PaymentId;

                    decimal authAmount = 0m;
                    decimal capturedAmount = 0m;
                    if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                    {
                        decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.authorizedAmount, out authAmount);
                    }

                    if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                    {
                        decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.totalAmount, out capturedAmount);
                    }

                    paymentData = new PaymentData
                    {
                        AuthorizationId = createPaymentResponse.AuthorizationId,
                        TransactionId = createPaymentResponse.Tid,
                        PaymentId = createPaymentResponse.PaymentId,
                        Value = authAmount,
                        RequestId = null,
                        CaptureId = null,
                        CreatePaymentResponse = createPaymentResponse,
                        CallbackUrl = createPaymentRequest.CallbackUrl,
                        OrderId = createPaymentRequest.OrderId
                    };

                    if (capturedAmount > 0)
                    {
                        paymentData.ImmediateCapture = true;
                        paymentData.CaptureId = paymentsResponse.Id;
                        paymentData.Value = capturedAmount;
                    }

                    await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
                }
                else
                {
                    _context.Vtex.Logger.Debug("CreatePayment", null, "Null Response");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CreatePayment", null,
                "Error ", ex,
                new[]
                {
                    ( "PaymentId", createPaymentRequest.PaymentId )
                });
            }

            return createPaymentResponse;
        }

        public async Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest cancelPaymentRequest)
        {
            CancelPaymentResponse cancelPaymentResponse = null;

            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(cancelPaymentRequest.PaymentId);
                if (paymentData.ImmediateCapture)
                {
                    // If transaction was auth & bill must refund, not reverse
                    RefundPaymentRequest refundPaymentRequest = new RefundPaymentRequest
                    {
                        PaymentId = cancelPaymentRequest.PaymentId,
                        RequestId = cancelPaymentRequest.RequestId,
                        SettleId = cancelPaymentRequest.AuthorizationId,
                        TransactionId = null,
                        Value = paymentData.Value
                    };

                    RefundPaymentResponse refundPaymentResponse = await this.RefundPayment(refundPaymentRequest);
                    cancelPaymentResponse = new CancelPaymentResponse();
                    cancelPaymentResponse.PaymentId = refundPaymentResponse.PaymentId;
                    cancelPaymentResponse.RequestId = refundPaymentResponse.RequestId;
                    cancelPaymentResponse.CancellationId = refundPaymentResponse.RefundId;
                    cancelPaymentResponse.Message = refundPaymentResponse.Message;
                    cancelPaymentResponse.Code = refundPaymentResponse.Code;

                    return cancelPaymentResponse;
                }

                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = await _vtexApiService.GetOrderId(cancelPaymentRequest.PaymentId),
                        applicationName = _context.Vtex.App.Name,
                        applicationVersion = _context.Vtex.App.Version,
                        applicationUser = _context.Vtex.App.Vendor,
                        partner = new Partner
                        {
                            solutionId = CybersourceConstants.SOLUTION_ID
                        }
                    },
                    reversalInformation = new ReversalInformation
                    {
                        amountDetails = new AmountDetails
                        {
                            totalAmount = paymentData.Value.ToString()
                        },
                        reason = "canceled"
                    }
                };

                PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessReversal(payment, paymentData.AuthorizationId);
                if (paymentsResponse != null)
                {
                    cancelPaymentResponse = new CancelPaymentResponse();
                    cancelPaymentResponse.PaymentId = cancelPaymentRequest.PaymentId;
                    cancelPaymentResponse.RequestId = cancelPaymentRequest.RequestId;
                    cancelPaymentResponse.CancellationId = paymentsResponse.Id;
                    cancelPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;
                    cancelPaymentResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status;
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CancelPayment", null,
                "Error ", ex,
                new[]
                {
                    ( "PaymentId", cancelPaymentRequest.PaymentId )
                });
            }

            return cancelPaymentResponse;
        }

        public async Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest)
        {
            CapturePaymentResponse capturePaymentResponse = null;

            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(capturePaymentRequest.PaymentId);

                if (paymentData == null)
                {
                    return capturePaymentResponse;
                }

                if (paymentData.CreatePaymentResponse != null)
                {
                    _context.Vtex.Logger.Debug("CapturePayment", null,
                    "Loaded PaymentData",
                    new[]
                    {
                        ( "orderId", paymentData.OrderId ),
                        ( "paymentId", paymentData.PaymentId ),
                        ( "paymentData", JsonConvert.SerializeObject(paymentData) )
                    });

                    if (paymentData.ImmediateCapture) // || !string.IsNullOrEmpty(paymentData.CaptureId))
                    {
                        capturePaymentResponse = new CapturePaymentResponse();
                        capturePaymentResponse.PaymentId = capturePaymentRequest.PaymentId;
                        capturePaymentResponse.RequestId = capturePaymentRequest.RequestId;
                        capturePaymentResponse.Code = paymentData.CreatePaymentResponse.Code;
                        capturePaymentResponse.Message = paymentData.CreatePaymentResponse.Message;
                        capturePaymentResponse.SettleId = paymentData.CreatePaymentResponse.AuthorizationId;
                        capturePaymentResponse.Value = paymentData.Value;

                        return capturePaymentResponse;
                    }
                }

                string referenceNumber = await _vtexApiService.GetOrderId(paymentData.OrderId);

                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = referenceNumber,
                        applicationName = _context.Vtex.App.Name,
                        applicationVersion = _context.Vtex.App.Version,
                        applicationUser = _context.Vtex.App.Vendor,
                        partner = new Partner
                        {
                            solutionId = CybersourceConstants.SOLUTION_ID
                        }
                    },
                    orderInformation = new OrderInformation
                    {
                        amountDetails = new AmountDetails
                        {
                            totalAmount = capturePaymentRequest.Value.ToString()
                        }
                    }
                };

                PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessCapture(payment, paymentData.AuthorizationId);
                if (paymentsResponse != null)
                {
                    capturePaymentResponse = new CapturePaymentResponse();
                    capturePaymentResponse.PaymentId = capturePaymentRequest.PaymentId;
                    capturePaymentResponse.RequestId = capturePaymentRequest.RequestId;
                    capturePaymentResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status;
                    capturePaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;
                    capturePaymentResponse.SettleId = paymentsResponse.Id;

                    decimal captureAmount = 0m;
                    if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                    {
                        decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.totalAmount, out captureAmount);
                    }
                    else
                    {
                        // Try to get transaction from Cybersource
                        CreateSearchRequest searchRequest = new CreateSearchRequest
                        {
                            Query = $"clientReferenceInformation.code:{referenceNumber}",
                            Sort = "submitTimeUtc:desc",
                            Limit = 2000
                        };

                        SearchResponse searchResponse = await _cybersourceApi.CreateSearchRequest(searchRequest);
                        if (searchResponse != null)
                        {
                            _context.Vtex.Logger.Debug("SearchResponse", null, "SearchResponse", new[] { ("orderId", paymentData.OrderId), ("paymentId", paymentData.PaymentId), ("searchResponse", JsonConvert.SerializeObject(searchResponse)) });
                            foreach (TransactionSummary transactionSummary in searchResponse.Embedded.TransactionSummaries)
                            {
                                if (transactionSummary.ApplicationInformation.Applications.Any(ai => ai.Name.Equals("ics_bill") && ai.ReasonCode.Equals("100")))
                                {
                                    string captureValueString = transactionSummary.OrderInformation.amountDetails.totalAmount;
                                    decimal captureValue = 0m;
                                    if (decimal.TryParse(captureValueString, out captureValue) && captureValue == capturePaymentRequest.Value)
                                    {
                                        capturePaymentResponse.Code = "100";
                                        capturePaymentResponse.Message = "Transaction retrieved from Cybersource.";
                                        capturePaymentResponse.SettleId = transactionSummary.Id;
                                        captureAmount = captureValue;
                                    }
                                }
                            }
                        }
                    }

                    capturePaymentResponse.Value = captureAmount;
                    paymentData.CaptureId = capturePaymentResponse.SettleId;
                    paymentData.Value = capturePaymentResponse.Value;
                    paymentData.TransactionId = capturePaymentResponse.PaymentId;

                    await _cybersourceRepository.SavePaymentData(capturePaymentRequest.PaymentId, paymentData);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CapturePayment", null,
                "Error ", ex,
                new[]
                {
                    ( "PaymentId", capturePaymentRequest.PaymentId )
                });
            }

            return capturePaymentResponse;
        }

        public async Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResponse refundPaymentResponse = null;

            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(refundPaymentRequest.PaymentId);
                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = await _vtexApiService.GetOrderId(refundPaymentRequest.PaymentId),
                        applicationName = _context.Vtex.App.Name,
                        applicationVersion = _context.Vtex.App.Version,
                        applicationUser = _context.Vtex.App.Vendor,
                        partner = new Partner
                        {
                            solutionId = CybersourceConstants.SOLUTION_ID
                        }
                    },
                    orderInformation = new OrderInformation
                    {
                        amountDetails = new AmountDetails
                        {
                            totalAmount = refundPaymentRequest.Value.ToString()
                        }
                    }
                };

                PaymentsResponse paymentsResponse = await _cybersourceApi.RefundCapture(payment, paymentData.CaptureId);
                if (paymentsResponse != null)
                {
                    refundPaymentResponse = new RefundPaymentResponse();
                    refundPaymentResponse.PaymentId = refundPaymentRequest.PaymentId;
                    refundPaymentResponse.RequestId = refundPaymentRequest.RequestId;
                    refundPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;
                    refundPaymentResponse.RefundId = paymentsResponse.Id;
                    refundPaymentResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status;

                    if (paymentsResponse.RefundAmountDetails != null && paymentsResponse.RefundAmountDetails.RefundAmount != null)
                    {
                        refundPaymentResponse.Value = decimal.Parse(paymentsResponse.RefundAmountDetails.RefundAmount);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("RefundPayment", null,
                "Error ", ex,
                new[]
                {
                    ( "PaymentId", refundPaymentRequest.PaymentId )
                });
            }

            return refundPaymentResponse;
        }
        #endregion Payments

        #region Antifraud
        public async Task<SendAntifraudDataResponse> SendAntifraudData(SendAntifraudDataRequest sendAntifraudDataRequest)
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;

            try
            {
                if (sendAntifraudDataRequest != null)
                {
                    Payments payment = new Payments
                    {
                        clientReferenceInformation = new ClientReferenceInformation
                        {
                            code = sendAntifraudDataRequest.Reference,
                            comments = sendAntifraudDataRequest.Id,
                            applicationName = _context.Vtex.App.Name,
                            applicationVersion = _context.Vtex.App.Version,
                            applicationUser = _context.Vtex.App.Vendor,
                            partner = new Partner
                            {
                                solutionId = CybersourceConstants.SOLUTION_ID
                            }
                        },
                        orderInformation = new OrderInformation
                        {
                            amountDetails = new AmountDetails
                            {
                                totalAmount = sendAntifraudDataRequest.Value.ToString()
                            },
                            billTo = new BillTo
                            {
                                firstName = sendAntifraudDataRequest.MiniCart.Buyer.FirstName,
                                lastName = sendAntifraudDataRequest.MiniCart.Buyer.LastName,
                                address1 = $"{sendAntifraudDataRequest.MiniCart.Buyer.Address.Number} {sendAntifraudDataRequest.MiniCart.Buyer.Address.Street}",
                                address2 = sendAntifraudDataRequest.MiniCart.Buyer.Address.Complement,
                                locality = sendAntifraudDataRequest.MiniCart.Buyer.Address.City ?? sendAntifraudDataRequest.MiniCart.Buyer.Address.Neighborhood,
                                administrativeArea = sendAntifraudDataRequest.MiniCart.Buyer.Address.State,
                                postalCode = sendAntifraudDataRequest.MiniCart.Buyer.Address.PostalCode,
                                country = this.GetCountryCode(sendAntifraudDataRequest.MiniCart.Buyer.Address.Country),
                                email = sendAntifraudDataRequest.MiniCart.Buyer.Email,
                                phoneNumber = sendAntifraudDataRequest.MiniCart.Buyer.Phone
                            },
                            shipTo = new ShipTo
                            {
                                address1 = $"{sendAntifraudDataRequest.MiniCart.Shipping.Address.Number} {sendAntifraudDataRequest.MiniCart.Shipping.Address.Street}",
                                address2 = sendAntifraudDataRequest.MiniCart.Shipping.Address.Complement,
                                administrativeArea = sendAntifraudDataRequest.MiniCart.Shipping.Address.State,
                                country = this.GetCountryCode(sendAntifraudDataRequest.MiniCart.Shipping.Address.Country),
                                postalCode = sendAntifraudDataRequest.MiniCart.Shipping.Address.PostalCode
                            },
                            lineItems = new List<LineItem>()
                        },
                        deviceInformation = new DeviceInformation
                        {
                            ipAddress = sendAntifraudDataRequest.Ip,
                            fingerprintSessionId = sendAntifraudDataRequest.DeviceFingerprint
                        }
                    };

                    foreach (AntifraudItem vtexItem in sendAntifraudDataRequest.MiniCart.Items)
                    {
                        LineItem lineItem = new LineItem
                        {
                            productSKU = vtexItem.Id,
                            productName = vtexItem.Name,
                            unitPrice = vtexItem.Price.ToString(),
                            quantity = vtexItem.Quantity.ToString(),
                            discountAmount = vtexItem.Discount.ToString()
                        };

                        payment.orderInformation.lineItems.Add(lineItem);
                    }

                    MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                    PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(sendAntifraudDataRequest);
                    string merchantName = string.Empty;
                    string merchantTaxId = string.Empty;
                    if (sendAntifraudDataRequest.MerchantSettings != null)
                    {
                        foreach (MerchantSetting merchantSetting in sendAntifraudDataRequest.MerchantSettings)
                        {
                            switch (merchantSetting.Name)
                            {
                                case CybersourceConstants.ManifestCustomField.CompanyName:
                                    merchantName = merchantSetting.Value;
                                    break;
                                case CybersourceConstants.ManifestCustomField.CompanyTaxId:
                                    merchantTaxId = merchantSetting.Value;
                                    break;
                                case CybersourceConstants.ManifestCustomField.MerchantId:
                                    if (!string.IsNullOrWhiteSpace(merchantSettings.MerchantId))
                                    {
                                        merchantSettings.MerchantId = merchantSetting.Value;
                                    }

                                    break;
                                case CybersourceConstants.ManifestCustomField.MerchantKey:
                                    if (!string.IsNullOrWhiteSpace(merchantSettings.MerchantKey))
                                    {
                                        merchantSettings.MerchantKey = merchantSetting.Value;
                                    }

                                    break;
                                case CybersourceConstants.ManifestCustomField.SharedSecretKey:
                                    if (!string.IsNullOrWhiteSpace(merchantSettings.SharedSecretKey))
                                    {
                                        merchantSettings.SharedSecretKey = merchantSetting.Value;
                                    }

                                    break;
                            }
                        }
                    }

                    requestWrapper.MerchantId = merchantSettings.MerchantId;
                    requestWrapper.CompanyName = merchantName;
                    requestWrapper.CompanyTaxId = merchantTaxId;
                    payment.merchantDefinedInformation = await this.GetMerchantDefinedInformation(merchantSettings, requestWrapper);

                    PaymentsResponse paymentsResponse = await _cybersourceApi.CreateDecisionManager(payment);

                    sendAntifraudDataResponse = new SendAntifraudDataResponse
                    {
                        Id = sendAntifraudDataRequest.Id,
                        Tid = paymentsResponse.Id,
                        Status = CybersourceConstants.VtexAntifraudStatus.Undefined,
                        //Score = paymentsResponse.RiskInformation != null ? double.Parse(paymentsResponse.RiskInformation.Score.Result) : 100d,
                        AnalysisType = CybersourceConstants.VtexAntifraudType.Automatic,
                        Responses = new Dictionary<string, string>(),
                        //Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status,
                        //Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message
                    };

                    switch (paymentsResponse.Status)
                    {
                        case "ACCEPTED":
                        case "PENDING_REVIEW":
                        case "PENDING_AUTHENTICATION":
                            sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Approved;   // Set Review to Approved otherwise auth will not be called
                            break;
                        case "INVALID_REQUEST":
                        case "CHALLENGE":
                            sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Undefined;
                            break;
                        case "REJECTED":
                        case "DECLINED":
                        case "AUTHENTICATION_FAILED":
                            sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Denied;
                            break;
                        default:
                            sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Undefined;
                            break;
                    }

                    string riskInfo = JsonConvert.SerializeObject(paymentsResponse.RiskInformation);
                    Dictionary<string, object> riskDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(riskInfo);
                    Func<Dictionary<string, object>, IEnumerable<KeyValuePair<string, object>>> flatten = null;
                    flatten = dict => dict.SelectMany(kv =>
                                kv.Value is Dictionary<string, object>
                                    ? flatten((Dictionary<string, object>)kv.Value)
                                    : new List<KeyValuePair<string, object>>() { kv }
                            );

                    sendAntifraudDataResponse.Responses = flatten(riskDictionary).ToDictionary(x => x.Key, x => x.Value.ToString());

                    sendAntifraudDataResponse.Score =
                        sendAntifraudDataResponse.Status.Equals(CybersourceConstants.VtexAntifraudStatus.Denied)
                            ? 99d
                            : 1d;

                    if (paymentsResponse.ErrorInformation != null)
                    {
                        sendAntifraudDataResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status;
                        sendAntifraudDataResponse.Message = paymentsResponse.ErrorInformation.Message;
                    }

                    if (sendAntifraudDataRequest != null)
                    {
                        await _cybersourceRepository.SaveAntifraudData(sendAntifraudDataRequest.Id, sendAntifraudDataResponse);
                    }
                }
                else
                {
                    _context.Vtex.Logger.Warn("SendAntifraudData", null, "Null request");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendAntifraudData", null,
                "Error ", ex,
                new[]
                {
                    ( "sendAntifraudDataRequest", JsonConvert.SerializeObject(sendAntifraudDataRequest) )
                });
            }

            return sendAntifraudDataResponse;
        }

        public async Task<SendAntifraudDataResponse> GetAntifraudStatus(string id)
        {
            return await _cybersourceRepository.GetAntifraudData(id);
        }
        #endregion Antifraud

        #region Payer Authentication
        public async Task<CreatePaymentResponse> SetupPayerAuth(CreatePaymentRequest createPaymentRequest)
        {
            CreatePaymentResponse createPaymentResponse = null;
            PaymentsResponse paymentsResponse = null;
            Payments payment = await this.BuildPayment(createPaymentRequest);

            try
            {
                paymentsResponse = await _cybersourceApi.SetupPayerAuth(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetupPayerAuth", null, "Error", ex);
            }

            createPaymentResponse = new CreatePaymentResponse
            {
                PaymentAppData = new PaymentAppData
                {
                    AppName = CybersourceConstants.PaymentFlowAppName,
                    Payload = JsonConvert.SerializeObject(paymentsResponse.ConsumerAuthenticationInformation)
                },
                PaymentId = createPaymentRequest.PaymentId,
                Status = CybersourceConstants.VtexAuthStatus.Undefined
            };

            return createPaymentResponse;
        }

        public async Task<CreatePaymentResponse> CheckPayerAuthEnrollment(CreatePaymentRequest createPaymentRequest)
        {
            CreatePaymentResponse createPaymentResponse = null;
            PaymentsResponse paymentsResponse = null;
            Payments payment = await this.BuildPayment(createPaymentRequest);

            try
            {
                paymentsResponse = await _cybersourceApi.CheckPayerAuthEnrollment(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CheckPayerAuthEnrollment", null, "Error", ex);
            }

            return createPaymentResponse;
        }
        #endregion

        #region Reporting
        public async Task<ConversionReportResponse> ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime)
        {
            return await _cybersourceApi.ConversionDetailReport(dtStartTime, dtEndTime);
        }

        public async Task<ConversionReportResponse> ConversionDetailReport(string startTime, string endTime)
        {
            DateTime dtStartTime = DateTime.Parse(startTime);
            DateTime dtEndTime = DateTime.Parse(endTime);
            return await this.ConversionDetailReport(dtStartTime, dtEndTime);
        }

        public async Task<string> RetrieveAvailableReports(DateTime dtStartTime, DateTime dtEndTime)
        {
            return await _cybersourceApi.RetrieveAvailableReports(dtStartTime, dtEndTime);
        }

        public async Task<string> RetrieveAvailableReports(string startTime, string endTime)
        {
            DateTime dtStartTime = DateTime.Parse(startTime);
            DateTime dtEndTime = DateTime.Parse(endTime);
            return await this.RetrieveAvailableReports(dtStartTime, dtEndTime);
        }

        public async Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime)
        {
            return await _cybersourceApi.GetPurchaseAndRefundDetails(dtStartTime, dtEndTime);
        }

        public async Task<string> GetPurchaseAndRefundDetails(string startTime, string endTime)
        {
            DateTime dtStartTime = DateTime.Parse(startTime);
            DateTime dtEndTime = DateTime.Parse(endTime);
            return await this.GetPurchaseAndRefundDetails(dtStartTime, dtEndTime);
        }
        #endregion Reporting

        #region OAuth
        public async Task<string> GetAuthUrl()
        {
            string authUrl = string.Empty;

            try
            {
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{CybersourceConstants.AUTH_SITE_BASE}/{CybersourceConstants.AUTH_APP_PATH}/{CybersourceConstants.AUTH_PATH}/{merchantSettings.IsLive}")
                };

                request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(CybersourceConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    authUrl = responseContent;
                    string siteUrl = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.FORWARDED_HOST];
                    authUrl = authUrl.Replace("state=", $"state={siteUrl}");
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetAuthUrl", null, $"Failed to get auth url [{response.StatusCode}]");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetAuthUrl", null, "Error :", ex);
            }

            return authUrl;
        }
        #endregion OAuth

        public async Task<bool> HealthCheck()
        {
            bool success = true;
            try
            {
                success &= await TestPayment();
                success &= await TestAntifraud();
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("HealthCheck", null, "Error:", ex);
            }
            return success;
        }

        private async Task<bool> TestPayment()
        {
            CreatePaymentRequest createPaymentRequest = new CreatePaymentRequest
            {

            };

            CreatePaymentResponse createPaymentResponse = await CreatePayment(createPaymentRequest);

            return createPaymentResponse != null;
        }

        private async Task<bool> TestAntifraud()
        {
            SendAntifraudDataRequest sendAntifraudDataRequest = new SendAntifraudDataRequest
            {

            };

            SendAntifraudDataResponse sendAntifraudDataResponse = await SendAntifraudData(sendAntifraudDataRequest);

            return sendAntifraudDataResponse != null;
        }

        public string GetAdministrativeArea(string region, string countryCode)
        {
            string regionCode = region;
            try
            {
                if (!string.IsNullOrEmpty(region))
                {
                    switch (countryCode)
                    {
                        case "CL": // Chile
                            regionCode = GetAdministrativeAreaChile(region);
                            break;
                        case "CO": // Colombia
                            regionCode = GetAdministrativeAreaColombia(region);
                            break;
                        case "PE": // Peru
                            regionCode = GetAdministrativeAreaPeru(region);
                            break;
                        case "MX": // Mexico
                            regionCode = GetAdministrativeAreaMexico(region);
                            break;
                    }

                    _context.Vtex.Logger.Debug("GetAdministrativeArea", null, $"'{region}', '{countryCode}' = '{regionCode}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetAdministrativeArea", null,
                "Error ", ex,
                new[]
                {
                    ( "region", region ),
                    ( "countryCode", countryCode )
                });
            }

            return regionCode;
        }

        public string GetAdministrativeAreaChile(string region)
        {
            string regionCode = "RM"; // Default to Región Metropolitana
            if (region.Contains("(") && region.Contains(")"))
            {
                string regionumber = region.Split('(', ')')[1];
                switch (regionumber)
                {
                    case "I": // Region de Tarapaca
                        regionCode = "TA";
                        break;
                    case "II": // Región de Antofagasta
                        regionCode = "AN";
                        break;
                    case "III": // Región de Atacama
                        regionCode = "AT";
                        break;
                    case "IV": // Región de Coquimbo
                        regionCode = "CO";
                        break;
                    case "V": // Región de Valparaíso
                        regionCode = "VS";
                        break;
                    case "VI": // Región del Libertador General Bernardo O’Higgins
                        regionCode = "LI";
                        break;
                    case "VII": // Región del Maule
                        regionCode = "ML";
                        break;
                    case "VIII": // Región del Biobío
                        regionCode = "BI";
                        break;
                    case "IX": // Región de La Araucanía
                        regionCode = "AR";
                        break;
                    case "X": // Región de Los Lagos
                        regionCode = "LL";
                        break;
                    case "XI": // Region de Aysen del General Carlos Ibanez del Campo
                        regionCode = "AI";
                        break;
                    case "XII": // Región de Magallanes y la Antártica Chilena
                        regionCode = "MA";
                        break;
                    case "":
                    case "XIII": // Región Metropolitana de Santiago
                        regionCode = "RM";
                        break;
                    case "XIV": // Región de Los Ríos
                        regionCode = "LR";
                        break;
                    case "XV": // Region de Arica y Parinacota
                        regionCode = "AP";
                        break;
                    case "XVI": // Región del Ñuble
                        regionCode = "NB";
                        break;
                    default:
                        regionCode = region;
                        break;
                }
            }

            return regionCode;
        }

        public string GetAdministrativeAreaColombia(string region)
        {
            string regionCode = string.Empty;
            region = region.Replace(" ", string.Empty);
            switch(region)
            {
                case "Distrito Capital de Bogotá":
                    regionCode = "DC";
                    break;
                case "Guaviare":
                    regionCode = "GUV";
                    break;
                case "Norte de Santander":
                    regionCode = "NSA";
                    break;
                case "San Andrés":
                    regionCode = "SAP";
                    break;
                case "Valle del Cauca":
                    regionCode = "VAC";
                    break;
                case "Vichada":
                    regionCode = "VID";
                    break;
                default:
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaPeru(string region)
        {
            string regionCode = string.Empty;
            region = region.Replace(" ", string.Empty);
            switch (region)
            {
                case "Huánuco":
                case "Huanuco":
                    regionCode = "HUC";
                    break;
                case "La Libertad":
                    regionCode = "LAL";
                    break;
                case "Madre de Dios":
                    regionCode = "MDD";
                    break;
                case "San Martín":
                    regionCode = "SAM";
                    break;
                case "Vichada":
                    regionCode = "VID";
                    break;
                default:
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaMexico(string region)
        {
            string regionCode = string.Empty;
            region = region.Replace(" ", string.Empty);
            switch (region)
            {
                case "Baja California":
                    regionCode = "BCN";
                    break;
                case "Baja California Sur":
                    regionCode = "BCS";
                    break;
                case "Chiapas":
                    regionCode = "CHP";
                    break;
                case "Chihuahua":
                    regionCode = "CHH";
                    break;
                case "Ciudad de México":
                case "Ciudad de Mexico":
                    regionCode = "DIF";
                    break;
                case "Guerrero":
                    regionCode = "GRO";
                    break;
                case "Nuevo León":
                case "Nuevo Leon":
                    regionCode = "NLE";
                    break;
                case "Quintana Roo":
                    regionCode = "ROO";
                    break;
                case "San Luis Potosí":
                    regionCode = "SLP";
                    break;
                default:
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        public string GetCountryCode(string country)
        {
            return CybersourceConstants.CountryCodesMapping[country];
        }

        private string GetCardType(string cardTypeText)
        {
            string cardType = null;
            switch (cardTypeText.ToLower())
            {
                case "visa":
                    cardType = "001";
                    break;
                case "mastercard":
                case "eurocard":
                    cardType = "002";
                    break;
                case "american express":
                case "amex":
                    cardType = "003";
                    break;
                case "discover":
                    cardType = "004";
                    break;
                case "diners":
                case "diners club":
                    cardType = "005";
                    break;
                case "carte blanche":
                    cardType = "006";
                    break;
                case "jcb":
                    cardType = "007";
                    break;
                case "enroute":
                    cardType = "014";
                    break;
                case "jal":
                    cardType = "021";
                    break;
                case "maestro uk":
                    cardType = "024";
                    break;
                case "delta":
                    cardType = "031";
                    break;
                case "visa electron":
                    cardType = "033";
                    break;
                case "dankort":
                    cardType = "034";
                    break;
                case "cartes bancaires":
                    cardType = "036";
                    break;
                case "carta si":
                    cardType = "037";
                    break;
                case "encoded account number":
                    cardType = "039";
                    break;
                case "uatp":
                    cardType = "040";
                    break;
                case "maestro international":
                    cardType = "042";
                    break;
                case "hipercard":
                    cardType = "050";
                    break;
                case "aura":
                    cardType = "051";
                    break;
                case "elo":
                    cardType = "054";
                    break;
                case "china unionpay":
                    cardType = "062";
                    break;
            }

            return cardType;
        }

        public CybersourceConstants.CardType FindType(string cardNumber)
        {
            // Visa:   / ^4(?!38935 | 011 | 51416 | 576)\d{ 12} (?:\d{ 3})?$/
            // Master: / ^5(?!04175 | 067 | 06699)\d{ 15}$/
            // Amex:   / ^3[47]\d{ 13}$/
            // Hiper:  / ^(38 | 60)\d{ 11} (?:\d{ 3})?(?:\d{ 3})?$/
            // Diners: / ^3(?:0[15] |[68]\d)\d{ 11}$/
            // Elo:    / ^[456](?:011 | 38935 | 51416 | 576 | 04175 | 067 | 06699 | 36368 | 36297)\d{ 10} (?:\d{ 2})?$/
            //https://www.regular-expressions.info/creditcard.html
            if (Regex.Match(cardNumber, @"^4[0-9]{5}$").Success)
            {
                return CybersourceConstants.CardType.Visa;
            }

            if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{3}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{3}|27[01][0-9]|2720)[0-9]$").Success)
            {
                return CybersourceConstants.CardType.MasterCard;
            }

            if (Regex.Match(cardNumber, @"^3[47][0-9]{4}$").Success)
            {
                return CybersourceConstants.CardType.AmericanExpress;
            }

            if (Regex.Match(cardNumber, @"^65[4-9][0-9]{3}|64[4-9][0-9]{3}|6011[0-9]{2}|(622(?:12[6-9]|1[3-9][0-9]|[2-8][0-9][0-9]|9[01][0-9]|92[0-5])[0-9]{3})$").Success)
            {
                return CybersourceConstants.CardType.Discover;
            }

            if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{1}$").Success)
            {
                return CybersourceConstants.CardType.JCB;
            }

            if (Regex.Match(cardNumber, @"^3(?:0[0-5]|[68][0-9])[0-9]{3}$").Success)
            {
                return CybersourceConstants.CardType.Diners;
            }

            //if (Regex.Match(cardNumber, @"^(636368|438935|504175|451416|636297|5067[0-9]{2}|4576[0-9]{2}|4011[0-9]{2})$").Success)
            if (Regex.Match(cardNumber, @"^[456](?:011|38935|51416|576|04175|067|06699|36368|36297)?$").Success)
            {
                return CybersourceConstants.CardType.Elo;
            }

            if (Regex.Match(cardNumber, @"^(38|60)\d{4}?$").Success)
            {
                return CybersourceConstants.CardType.Hipercard;
            }

            return CybersourceConstants.CardType.Unknown;
        }

        public object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                foreach (var prop in propertyName.Split('.').Select(s => obj.GetType().GetProperty(s)))
                {
                    if (prop.Name == "CustomApps")
                    {
                        var json = JsonConvert.SerializeObject(obj);
                        var customDataWrapper = JsonConvert.DeserializeObject<CustomDataWrapper>(json);
                        JObject dictObj = customDataWrapper.CustomApps;
                        Dictionary<string, string> dictionary = dictObj.ToObject<Dictionary<string, string>>();
                        string customFieldKey = propertyName.Split('.').Last();
                        if (dictionary.Keys.Contains(customFieldKey))
                        {
                            obj = dictionary[customFieldKey];
                        }
                        else
                        {
                            obj = string.Empty;
                        }
                    }
                    else
                    {
                        obj = prop.GetValue(obj, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetPropertyValue", null,
                "Error: ", ex,
                new[]
                {
                    ( "Could not get value of ", propertyName ),
                    ( "object", JsonConvert.SerializeObject(obj) )
                });
            }

            return obj;
        }

        public async Task<List<MerchantDefinedInformation>> GetMerchantDefinedInformation(MerchantSettings merchantSettings, PaymentRequestWrapper requestWrapper)
        {
            List<MerchantDefinedInformation> merchantDefinedInformationList = new List<MerchantDefinedInformation>();

            string startCharacter = "{{";
            string endCharacter = "}}";
            string valueSeparator = "|";
            try
            {
                if (merchantSettings.MerchantDefinedValueSettings != null)
                {
                    MerchantDefinedInformation merchantDefinedInformation = new MerchantDefinedInformation();
                    int merchantDefinedValueKey = 0;

                    foreach (MerchantDefinedValueSetting merchantDefinedValueSetting in merchantSettings.MerchantDefinedValueSettings)
                    {
                        merchantDefinedValueKey++;

                        if (merchantDefinedValueSetting.IsValid)
                        {
                            string merchantDefinedValue = merchantDefinedValueSetting.UserInput;

                            if (!string.IsNullOrEmpty(merchantDefinedValue))
                            {
                                try
                                {
                                    if (merchantDefinedValue.Contains(startCharacter) && merchantDefinedValue.Contains(endCharacter))
                                    {
                                        int sanityCheck = 0; // prevent infinate loops jic
                                        do
                                        {
                                            int start = merchantDefinedValue.IndexOf(startCharacter) + startCharacter.Length;
                                            string valueSubStr = merchantDefinedValue.Substring(start, merchantDefinedValue.IndexOf(endCharacter) - start);
                                            string originalValueSubStr = valueSubStr;
                                            string propValue = string.Empty;
                                            if (!string.IsNullOrEmpty(valueSubStr))
                                            {
                                                if (valueSubStr.Contains(valueSeparator))
                                                {
                                                    string[] valueSubStrArr = valueSubStr.Split(valueSeparator);
                                                    valueSubStr = valueSubStrArr[0];
                                                    if (!string.IsNullOrEmpty(valueSubStr))
                                                    {
                                                        propValue = this.GetPropertyValue(requestWrapper, valueSubStr).ToString();
                                                    }

                                                    string operation = valueSubStrArr[1];
                                                    if (!string.IsNullOrEmpty(operation))
                                                    {
                                                        switch (operation.ToUpper())
                                                        {
                                                            case "PAD":
                                                                string[] paddArr = valueSubStrArr[2].Split(':');
                                                                int totalLength = 0;
                                                                if (int.TryParse(paddArr[0], out totalLength))
                                                                {
                                                                    if (propValue.Length < totalLength)
                                                                    {
                                                                        char padChar = paddArr[1].ToCharArray().First();
                                                                        propValue = propValue.PadLeft(totalLength, padChar);
                                                                    }
                                                                    else if (propValue.Length > totalLength)
                                                                    {
                                                                        propValue = propValue.Substring(0, totalLength);
                                                                    }
                                                                }

                                                                break;
                                                            case "TRIM":
                                                                int trimLength = 0;
                                                                if (int.TryParse(valueSubStrArr[2], out trimLength))
                                                                {
                                                                    int currentLength = propValue.Length;
                                                                    int offset = Math.Max(0, currentLength - trimLength);
                                                                    propValue = propValue.Substring(offset);
                                                                }

                                                                break;
                                                            case "DATE":
                                                                try
                                                                {
                                                                    DateTime dt = DateTime.Now;
                                                                    string dateFormat = valueSubStrArr[2];
                                                                    propValue = dt.ToString(dateFormat);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _context.Vtex.Logger.Error("GetMerchantDefinedInformation", "Date", JsonConvert.SerializeObject(valueSubStrArr), ex);
                                                                }

                                                                break;
                                                            case "AGE":
                                                                try
                                                                {
                                                                    DateTime propAsDate;
                                                                    string timespanFormat = valueSubStrArr[2];
                                                                    if (DateTime.TryParse(propValue, out propAsDate))
                                                                    {
                                                                        TimeSpan dateDiff = DateTime.Now - propAsDate;
                                                                        propValue = dateDiff.ToString(timespanFormat);
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _context.Vtex.Logger.Error("GetMerchantDefinedInformation", "Age", JsonConvert.SerializeObject(valueSubStrArr), ex);
                                                                }

                                                                break;
                                                            case "EQUALS":
                                                                bool match = string.Equals(propValue, valueSubStrArr[2], StringComparison.OrdinalIgnoreCase);
                                                                propValue = match.ToString();

                                                                break;
                                                            default:
                                                                _context.Vtex.Logger.Warn("GetMerchantDefinedInformation", null, $"Invalid operation '{operation}'");
                                                                break;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    var propValueObj = this.GetPropertyValue(requestWrapper, valueSubStr);
                                                    if (propValueObj != null)
                                                    {
                                                        propValue = propValueObj.ToString();
                                                    }
                                                }
                                            }

                                            merchantDefinedValue = merchantDefinedValue.Replace($"{startCharacter}{originalValueSubStr}{endCharacter}", propValue);
                                            sanityCheck = sanityCheck + 1;
                                        }
                                        while (merchantDefinedValue.Contains(startCharacter) && merchantDefinedValue.Contains(endCharacter) && sanityCheck < 100);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _context.Vtex.Logger.Error("GetMerchantDefinedInformation", null, $"Error parsing MDV '{merchantDefinedValue}'", ex);
                                }

                                merchantDefinedInformation = new MerchantDefinedInformation
                                {
                                    key = merchantDefinedValueKey,
                                    value = merchantDefinedValue
                                };
                            }
                            else
                            {
                                merchantDefinedInformation = new MerchantDefinedInformation
                                {
                                    key = merchantDefinedValueKey,
                                    value = string.Empty
                                };
                            }
                        }
                        else
                        {
                            merchantDefinedInformation = new MerchantDefinedInformation
                            {
                                key = merchantDefinedValueKey,
                                value = string.Empty
                            };
                        }

                        try
                        {
                            merchantDefinedInformationList.Add(merchantDefinedInformation);
                        }
                        catch (Exception ex)
                        {
                            _context.Vtex.Logger.Error("GetMerchantDefinedInformation", null, $"Error adding '{merchantDefinedInformation.key}:{merchantDefinedInformation.value}'", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetMerchantDefinedInformation", null,
                "Error: ", ex,
                new[]
                {
                    ( "requestWrapper", JsonConvert.SerializeObject(requestWrapper) )
                });
            }

            return merchantDefinedInformationList;
        }

        public void BinLookup(string cardBin, string paymentMethod, out bool isDebit, out string cardType, out CybersourceConstants.CardType cardBrandName)
        {
            isDebit = false;
            CybersourceBinLookupResponse cybersourceBinLookup = _cybersourceApi.BinLookup(cardBin).Result;
            if (cybersourceBinLookup != null && cybersourceBinLookup.PaymentAccountInformation != null && cybersourceBinLookup.PaymentAccountInformation.Card != null)
            {
                cardType = cybersourceBinLookup.PaymentAccountInformation.Card.Type;
                if (!Enum.TryParse(cybersourceBinLookup.PaymentAccountInformation.Card.BrandName, true, out cardBrandName))
                {
                    cardBrandName = this.FindType(cardBin);
                }

                if (cybersourceBinLookup.PaymentAccountInformation.Features != null && cybersourceBinLookup.PaymentAccountInformation.Features.AccountFundingSource != null)
                {
                    isDebit = cybersourceBinLookup.PaymentAccountInformation.Features.AccountFundingSource.ToUpper().Equals("DEBIT");
                }
            }
            else
            {
                BinLookup binLookup = _vtexApiService.BinLookup(cardBin).Result;
                if (binLookup != null && binLookup.Type != null && binLookup.Scheme != null)
                {
                    isDebit = binLookup.Type.ToLower().Equals("debit");
                    cardType = this.GetCardType(binLookup.Scheme);
                    if (!Enum.TryParse(binLookup.Scheme, true, out cardBrandName))
                    {
                        cardBrandName = this.FindType(cardBin);
                    }
                }
                else
                {
                    cardType = this.GetCardType(paymentMethod);
                    cardBrandName = this.FindType(cardBin);
                }
            }
        }
    }
}