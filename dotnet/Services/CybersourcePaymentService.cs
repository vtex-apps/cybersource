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
        /// <summary>
        /// Build request body for Cybersource API
        /// </summary>
        /// <param name="createPaymentRequest"></param>
        /// <returns></returns>
        public async Task<Payments> BuildPayment(CreatePaymentRequest createPaymentRequest, MerchantSettings merchantSettings)
        {
            Payments payment = null;
            try
            {
                string merchantName = createPaymentRequest.MerchantName;
                string merchantTaxId = string.Empty;
                bool doCapture = false;
                string orderSuffix = string.Empty;
                if(!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                {
                    orderSuffix = merchantSettings.OrderSuffix.Trim();
                }

                (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, createPaymentRequest.MerchantSettings, merchantName);

                string referenceNumber = await _vtexApiService.GetOrderId(createPaymentRequest.Reference, createPaymentRequest.OrderId);
                this.BinLookup(createPaymentRequest.Card.Bin, createPaymentRequest.PaymentMethod, out bool isDebit, out string cardType, out CybersourceConstants.CardType cardBrandName, merchantSettings);
                payment = new Payments
                {
                    merchantInformation = new MerchantInformation
                    {
                        merchantName = merchantName,
                        taxId = merchantTaxId
                    },
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = $"{referenceNumber}{orderSuffix}", // Use Reference to have a consistent number with Fraud?
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
                            taxAmount = createPaymentRequest.MiniCart.TaxValue.ToString("0.00"),
                            freightAmount = createPaymentRequest.MiniCart.ShippingValue.ToString("0.00")
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
                        fingerprintSessionId = merchantSettings.UseOrderIdForFingerprint ? createPaymentRequest.OrderId : createPaymentRequest.DeviceFingerprint
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
                PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(createPaymentRequest)
                {
                    MerchantId = merchantSettings.MerchantId,
                    CompanyName = merchantName,
                    CompanyTaxId = merchantTaxId
                };

                payment.processingInformation = new ProcessingInformation();
                if (doCapture)
                {
                    payment.processingInformation.capture = "true";
                }

                if(isDebit && createPaymentRequest.Installments > 0)
                {
                    _context.Vtex.Logger.Info("BuildPayment", "Installments", "Card is Debit - Setting Installments to one.", new[] { ("orderId", createPaymentRequest.OrderId), ("Installments", createPaymentRequest.Installments.ToString()) });
                    createPaymentRequest.Installments = 1;
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
                            payment.processingInformation.reconciliationId = await this.GetReconciliationId(merchantSettings, createPaymentRequest);
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
                        payment.processingInformation.reconciliationId = await this.GetReconciliationId(merchantSettings, createPaymentRequest);
                        if (createPaymentRequest.Installments > 1)
                        {
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalCount = numberOfInstallments,
                                planType = "01"
                            };
                        }
                        else
                        {
                            payment.processingInformation.commerceIndicator = CybersourceConstants.INTERNET;
                        }

                        break;
                    case CybersourceConstants.Processors.AmexDirect:
                        payment.processingInformation.commerceIndicator = CybersourceConstants.INSTALLMENT;
                        payment.processingInformation.reconciliationId = await this.GetReconciliationId(merchantSettings, createPaymentRequest);
                        payment.installmentInformation = new InstallmentInformation
                        {
                            totalCount = numberOfInstallments,
                            planType = "02"
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
                        payment.processingInformation.reconciliationId = await this.GetReconciliationId(merchantSettings, createPaymentRequest);
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

                // Load order group in case the order was split
                VtexOrder[] vtexOrders = await _vtexApiService.GetOrderGroup(createPaymentRequest.OrderId);
                List<VtexOrderItem> vtexOrderItems = new List<VtexOrderItem>();
                if (vtexOrders != null)
                {
                    foreach (VtexOrder vtexOrder in vtexOrders)
                    {
                        // ContextData is not returned in the order group list
                        if (merchantSettings.MerchantDefinedValueSettings.Exists(ms => ms.UserInput.Contains("ContextData")) || merchantSettings.MerchantDefinedValueSettings.Exists(ms => ms.UserInput.Contains("PersonalData")))
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

                        if (merchantSettings.MerchantDefinedValueSettings.Exists(ms => ms.UserInput.Contains("ClientProfileData")))
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

                        if (merchantSettings.MerchantDefinedValueSettings.Exists(ms => ms.UserInput.Contains("Shipping")))
                        {
                            LogisticsInfo logisticsInfo = vtexOrder.ShippingData.LogisticsInfo.FirstOrDefault();
                            Sla selectedSla = logisticsInfo.Slas.Find(s => s.Id.Equals(logisticsInfo.SelectedSla, StringComparison.InvariantCultureIgnoreCase));
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

                this.GetItemTaxAmounts(merchantSettings, vtexOrderItems, payment, createPaymentRequest);

                if (merchantSettings.MerchantDefinedValueSettings.Exists(ms => ms.UserInput.Contains("AdditionalData")))
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

        /// <summary>
        /// Process a Payment
        /// /pts/v2/payments
        /// </summary>
        /// <param name="createPaymentRequest"></param>
        /// <param name="authenticationTransactionId"></param>
        /// <param name="consumerAuthenticationInformation"></param>
        /// <returns></returns>
        public async Task<(CreatePaymentResponse, PaymentsResponse)> CreatePayment(CreatePaymentRequest createPaymentRequest, string authenticationTransactionId = null, ConsumerAuthenticationInformation consumerAuthenticationInformation = null, MerchantSettings merchantSettings = null)
        {
            CreatePaymentResponse createPaymentResponse = null;
            PaymentsResponse paymentsResponse = null;
            try
            {
                bool doCapture = false;
                if (merchantSettings == null)
                {
                    merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                    string merchantName = createPaymentRequest.MerchantName;
                    string merchantTaxId = string.Empty;
                    (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, createPaymentRequest.MerchantSettings, merchantName);
                }

                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(createPaymentRequest.PaymentId);
                if (paymentData == null)
                {
                    paymentData = new PaymentData
                    {
                        CreatePaymentRequest = createPaymentRequest
                    };
                }
                else if (paymentData.CreatePaymentResponse != null && string.IsNullOrEmpty(authenticationTransactionId) && paymentData.CreatePaymentResponse.Status != null)
                {
                    await _vtexApiService.ProcessConversions();
                    return (paymentData.CreatePaymentResponse, null);
                }
                else if (paymentData.TimedOut)
                {
                    try
                    {
                        if (createPaymentRequest.MerchantSettings != null)
                        {
                            MerchantSetting merchantSettingAuthAndBill = createPaymentRequest.MerchantSettings.Find(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.CaptureSetting));
                            if (merchantSettingAuthAndBill != null && merchantSettingAuthAndBill.Value != null && merchantSettingAuthAndBill.Value.Equals(CybersourceConstants.CaptureSetting.ImmediateCapture, StringComparison.OrdinalIgnoreCase))
                            {
                                // Need to check if there has already been a successful capture
                                string referenceNumber = await _vtexApiService.GetOrderId(paymentData.CreatePaymentRequest.OrderId);
                                string orderSuffix = string.Empty;
                                if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                                {
                                    orderSuffix = merchantSettings.OrderSuffix.Trim();
                                }

                                SearchResponse searchResponse = await this.SearchTransaction($"{referenceNumber}{orderSuffix}", merchantSettings);
                                if (searchResponse != null)
                                {
                                    createPaymentResponse = new CreatePaymentResponse();
                                    createPaymentResponse.PaymentId = createPaymentRequest.PaymentId;
                                    createPaymentResponse.Status = CybersourceConstants.VtexAuthStatus.Denied;
                                    foreach (var transactionSummary in searchResponse.Embedded.TransactionSummaries.Where(transactionSummary => transactionSummary.ApplicationInformation.Applications.Exists(ai => ai.Name.Equals(CybersourceConstants.Applications.Capture) && ai.ReasonCode.Equals("100"))))
                                    {
                                        string captureValueString = transactionSummary.OrderInformation.amountDetails.totalAmount;
                                        if (decimal.TryParse(captureValueString, out decimal captureValue) && captureValue == createPaymentRequest.Value)
                                        {
                                            createPaymentResponse.Status = CybersourceConstants.VtexAuthStatus.Approved;
                                            createPaymentResponse.Code = "100";
                                            createPaymentResponse.Message = "Transaction retrieved from Cybersource.";
                                            createPaymentResponse.AuthorizationId = transactionSummary.Id;
                                            paymentData.AuthorizationId = createPaymentResponse.AuthorizationId;
                                            paymentData.TransactionId = createPaymentResponse.Tid;
                                            paymentData.PaymentId = createPaymentResponse.PaymentId;
                                            paymentData.Value = captureValue;
                                            paymentData.RequestId = null;
                                            paymentData.CaptureId = null;
                                            paymentData.CreatePaymentResponse = createPaymentResponse;
                                            paymentData.ImmediateCapture = true;
                                            paymentData.CaptureId = paymentsResponse.Id;
                                            paymentData.Value = captureValue;
                                        }
                                    }

                                    await _cybersourceRepository.SavePaymentData(paymentData.PaymentId, paymentData);
                                    return (createPaymentResponse, paymentsResponse);
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _context.Vtex.Logger.Error("CreatePayment", "TransactionSummaries",
                        "Error ", ex,
                        new[]
                        {
                            ( "PaymentId", createPaymentRequest.PaymentId )
                        });
                    }
                }

                if(doCapture)
                {
                    // Setting the TimedOut flag to true so that any follow up requests will check for previous captures
                    paymentData.TimedOut = true;
                    await _cybersourceRepository.SavePaymentData(paymentData.PaymentId, paymentData);
                }
                
                bool isPayerAuth = false;
                Payments payment = await this.BuildPayment(createPaymentRequest, merchantSettings);
                try
                {
                    ConsumerAuthenticationInformation consumerAuthenticationInformationToCopy = consumerAuthenticationInformation ?? paymentData.ConsumerAuthenticationInformation;
                    if (consumerAuthenticationInformationToCopy != null)
                    {
                        payment.consumerAuthenticationInformation = new ConsumerAuthenticationInformation
                        {
                            Eci = consumerAuthenticationInformationToCopy.Eci,
                            EciRaw = consumerAuthenticationInformationToCopy.EciRaw,
                            Cavv = consumerAuthenticationInformationToCopy.Cavv,
                            Xid = consumerAuthenticationInformationToCopy.Xid,
                            EcommerceIndicator = consumerAuthenticationInformationToCopy.EcommerceIndicator,
                            PaSpecificationVersion = consumerAuthenticationInformationToCopy.SpecificationVersion,
                            DirectoryServerTransactionId = consumerAuthenticationInformationToCopy.DirectoryServerTransactionId,
                            UcafCollectionIndicator = consumerAuthenticationInformationToCopy.UcafCollectionIndicator,
                            UcafAuthenticationData = consumerAuthenticationInformationToCopy.UcafAuthenticationData
                        };

                        if (!string.IsNullOrWhiteSpace(consumerAuthenticationInformationToCopy.EcommerceIndicator))
                        {
                            payment.processingInformation.commerceIndicator = consumerAuthenticationInformationToCopy.EcommerceIndicator;
                        }
                        else if (!string.IsNullOrWhiteSpace(consumerAuthenticationInformationToCopy.Indicator))
                        {
                            payment.processingInformation.commerceIndicator = consumerAuthenticationInformationToCopy.Indicator;
                        }

                        _context.Vtex.Logger.Debug("CreatePayment", "ConsumerAuthenticationInformation", $"{JsonConvert.SerializeObject(payment.consumerAuthenticationInformation)} | CommerceIndicator: {payment.processingInformation.commerceIndicator}");
                    }
                }
                catch(Exception ex)
                {
                    _context.Vtex.Logger.Error("CreatePayment", "ConsumerAuthenticationInformation",
                    "Error ", ex,
                    new[]
                    {
                        ( "PaymentId", createPaymentRequest.PaymentId )
                    });
                }

                paymentsResponse = await _cybersourceApi.ProcessPayment(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl, merchantSettings);

                if (paymentsResponse != null)
                {
                    createPaymentResponse = new CreatePaymentResponse();
                    string paymentStatus;
                    bool doCancel;
                    (createPaymentResponse, paymentsResponse, paymentStatus, doCancel) = await this.GetPaymentStatus(createPaymentResponse, createPaymentRequest, paymentsResponse, isPayerAuth);

                    createPaymentResponse.AuthorizationId = paymentsResponse.Id;
                    createPaymentResponse.Tid = paymentsResponse.Id;
                    createPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message ?? paymentsResponse.Status;

                    var errorInformation = paymentsResponse.ErrorInformation != null
                        ? paymentsResponse.ErrorInformation.Reason
                        : paymentsResponse.Status;

                    createPaymentResponse.Code = paymentsResponse.ProcessorInformation != null
                        ? paymentsResponse.ProcessorInformation.ResponseCode
                        : errorInformation;

                    if (paymentsResponse.ProcessorInformation != null)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(merchantSettings.CustomNsu))
                            {
                                createPaymentResponse.Nsu = await this.GetReconciliationId(merchantSettings, createPaymentRequest);
                            }
                            else
                            {
                                createPaymentResponse.Nsu = paymentsResponse.ProcessorInformation.TransactionId;
                            }
                        }
                        catch(Exception ex)
                        {
                            _context.Vtex.Logger.Error("CreatePayment", "ProcessorInformation.TransactionId", "Error setting NSU", ex, new[] { ("OrderId", createPaymentRequest.OrderId) });
                        }
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

                    paymentData.AuthorizationId = createPaymentResponse.AuthorizationId;
                    paymentData.TransactionId = createPaymentResponse.Tid;
                    paymentData.PaymentId = createPaymentResponse.PaymentId;
                    paymentData.Value = authAmount;
                    paymentData.RequestId = null;
                    paymentData.CaptureId = null;
                    paymentData.CreatePaymentResponse = createPaymentResponse;

                    if (capturedAmount > 0)
                    {
                        paymentData.ImmediateCapture = true;
                        paymentData.CaptureId = paymentsResponse.Id;
                        paymentData.Value = capturedAmount;
                    }

                    await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
                    if (doCancel)
                    {
                        // Reverse Authorization due to failed 3DS condition
                        CancelPaymentRequest cancelPaymentRequest = new CancelPaymentRequest
                        {
                            AuthorizationId = paymentData.AuthorizationId,
                            PaymentId = paymentData.PaymentId,
                            RequestId = paymentData.RequestId
                        };

                        await this.CancelPayment(cancelPaymentRequest);
                    }
                }
                else
                {
                    _context.Vtex.Logger.Warn("CreatePayment", null, "Null Response");
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

            _context.Vtex.Logger.Info("CreatePayment", "Response", createPaymentRequest.OrderId, new[] {
                ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)),
                ("createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse)),
                ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse)) });
            
            return (createPaymentResponse, paymentsResponse);
        }

        /// <summary>
        /// Cancel Payment
        /// </summary>
        /// <param name="cancelPaymentRequest"></param>
        /// <returns></returns>
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
                    cancelPaymentResponse = new CancelPaymentResponse
                    {
                        PaymentId = refundPaymentResponse.PaymentId,
                        RequestId = refundPaymentResponse.RequestId,
                        CancellationId = refundPaymentResponse.RefundId,
                        Message = refundPaymentResponse.Message,
                        Code = refundPaymentResponse.Code
                    };

                    return cancelPaymentResponse;
                }

                string referenceNumber = await _vtexApiService.GetOrderId(paymentData.CreatePaymentRequest.OrderId);
                string orderSuffix = string.Empty;
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                string merchantName = paymentData.CreatePaymentRequest.MerchantName;
                string merchantTaxId = string.Empty;
                bool doCapture = false;
                (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, paymentData.CreatePaymentRequest.MerchantSettings, merchantName);
                if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                {
                    orderSuffix = merchantSettings.OrderSuffix.Trim();
                }

                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = $"{referenceNumber}{orderSuffix}",
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
                        //reason = "34" // 34: suspected fraud
                    }
                };

                PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessReversal(payment, paymentData.AuthorizationId, merchantSettings);
                if (paymentsResponse != null)
                {
                    cancelPaymentResponse = new CancelPaymentResponse
                    {
                        PaymentId = cancelPaymentRequest.PaymentId,
                        RequestId = cancelPaymentRequest.RequestId,
                        CancellationId = paymentsResponse.Id,
                        Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message,
                        Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status
                    };
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

        /// <summary>
        /// Capture Payment
        /// </summary>
        /// <param name="capturePaymentRequest"></param>
        /// <returns></returns>
        public async Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest)
        {
            CapturePaymentResponse capturePaymentResponse = null;

            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(capturePaymentRequest.PaymentId);

                if (paymentData == null)
                {
                    _context.Vtex.Logger.Error("CapturePayment", null,
                    "Could not load Payment Data ", null,
                    new[]
                    {
                        ( "PaymentId", capturePaymentRequest.PaymentId )
                    });
                }

                if (paymentData != null && paymentData.CreatePaymentResponse != null && paymentData.ImmediateCapture)
                {
                    capturePaymentResponse = new CapturePaymentResponse
                    {
                        PaymentId = capturePaymentRequest.PaymentId,
                        RequestId = capturePaymentRequest.RequestId,
                        Code = paymentData.CreatePaymentResponse.Code,
                        Message = paymentData.CreatePaymentResponse.Message,
                        SettleId = paymentData.CreatePaymentResponse.AuthorizationId,
                        Value = paymentData.Value
                    };

                    return capturePaymentResponse;
                }

                string referenceNumber = capturePaymentRequest.TransactionId;
                if (paymentData != null)
                {
                    if (!string.IsNullOrEmpty(paymentData.OrderId))
                    {
                        referenceNumber = paymentData.OrderId;
                    }
                    else if (paymentData.CreatePaymentRequest != null && !string.IsNullOrEmpty(paymentData.CreatePaymentRequest.OrderId))
                    {
                        referenceNumber = paymentData.CreatePaymentRequest.OrderId;
                    }
                }

                string orderSuffix = string.Empty;
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                string merchantName = string.Empty;
                if (paymentData != null && paymentData.CreatePaymentResponse != null && !string.IsNullOrEmpty(paymentData.CreatePaymentRequest.MerchantName))
                {
                    merchantName = paymentData.CreatePaymentRequest.MerchantName;
                }

                string merchantTaxId = string.Empty;
                bool doCapture = false;
                if (paymentData != null && paymentData.CreatePaymentRequest != null && paymentData.CreatePaymentRequest.MerchantSettings != null)
                {
                    (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, paymentData.CreatePaymentRequest.MerchantSettings, merchantName);
                }

                if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                {
                    orderSuffix = merchantSettings.OrderSuffix.Trim();
                }

                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = $"{referenceNumber}{orderSuffix}",
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

                string authId = capturePaymentRequest.AuthorizationId;
                if (paymentData != null && !string.IsNullOrEmpty(paymentData.AuthorizationId))
                {
                    authId = paymentData.AuthorizationId;
                }

                #region Custom Payload
                if (paymentData != null && merchantSettings.Region != null && merchantSettings.Region.Equals(CybersourceConstants.Regions.Ecuador))
                {
                    try
                    {
                        bool captureFullAmount = false;
                        if (capturePaymentRequest.Value.Equals(paymentData.CreatePaymentRequest.Value))
                        {
                            captureFullAmount = true;
                        }

                        payment.orderInformation.amountDetails.nationalTaxIncluded = "1";
                        payment.orderInformation.lineItems = new List<LineItem>();
                        VtexOrder[] vtexOrders = await _vtexApiService.GetOrderGroup(paymentData.CreatePaymentRequest.OrderId);
                        if (vtexOrders != null)
                        {
                            List<VtexOrderItem> vtexOrderItems = new List<VtexOrderItem>();
                            Dictionary<string, List<VtexOrderItem>> shippedItemsWithDate = new Dictionary<string, List<VtexOrderItem>>();
                            foreach (VtexOrder vtexGroupOrder in vtexOrders)
                            {
                                foreach (VtexOrderItem vtexItem in vtexGroupOrder.Items)
                                {
                                    if (!vtexOrderItems.Contains(vtexItem))
                                    {
                                        vtexOrderItems.Add(vtexItem);
                                    }
                                }

                                if (!captureFullAmount)
                                {
                                    // If not capturing the full amount we need to find the shipment info
                                    VtexOrder vtexOrder = await _vtexApiService.GetOrderInformation(vtexGroupOrder.OrderId, true);
                                    if (vtexOrder != null && vtexOrder.PackageAttachment != null && vtexOrder.PackageAttachment.Packages != null)
                                    {
                                        foreach (Package package in vtexOrder.PackageAttachment.Packages.Reverse<Package>())
                                        {
                                            List<VtexOrderItem> shippedItems = new List<VtexOrderItem>();
                                            foreach (PackageItem packageItem in package.Items)
                                            {
                                                VtexOrderItem vtexOrderItem = vtexOrder.Items[(int)packageItem.ItemIndex];
                                                vtexOrderItem.Quantity = packageItem.Quantity;
                                                vtexOrderItem.Price = packageItem.Price;
                                                vtexOrderItem.UnitMultiplier = packageItem.UnitMultiplier;

                                                shippedItems.Add(vtexOrderItem);
                                            }

                                            shippedItemsWithDate.Add(package.IssuanceDate, shippedItems);
                                        }
                                    }
                                }
                            }

                            this.GetItemTaxAmounts(merchantSettings, vtexOrderItems, payment, paymentData.CreatePaymentRequest);

                            if (!captureFullAmount)
                            {
                                // Adjust for partial shipment
                                List<VtexOrderItem> lastShipment = shippedItemsWithDate.OrderByDescending(d => d.Key).FirstOrDefault().Value;
                                List<LineItem> itemsToRemove = new List<LineItem>();
                                foreach (LineItem lineItem in payment.orderInformation.lineItems)
                                {
                                    if (lastShipment.Select(s => s.SkuName).Contains(lineItem.productSKU))
                                    {
                                        VtexOrderItem shippedItem = lastShipment.Find(si => si.SkuName.Equals(lineItem.productSKU));
                                        long originalQuantity = long.Parse(lineItem.quantity);
                                        decimal percentOfTotal = (decimal)shippedItem.Quantity / originalQuantity;
                                        lineItem.quantity = shippedItem.Quantity.ToString();
                                        lineItem.taxAmount = (decimal.Parse(lineItem.taxAmount) * percentOfTotal).ToString("0.00");
                                        foreach (TaxDetail taxDetail in lineItem.taxDetails)
                                        {
                                            taxDetail.amount = (decimal.Parse(taxDetail.amount) * percentOfTotal).ToString("0.00");
                                        }
                                    }
                                    else
                                    {
                                        itemsToRemove.Add(lineItem);
                                    }
                                }

                                foreach (LineItem itemToRemove in itemsToRemove)
                                {
                                    payment.orderInformation.lineItems.Remove(itemToRemove);
                                }
                            }
                        }

                        payment.orderInformation.invoiceDetails = new InvoiceDetails
                        {
                            purchaseOrderNumber = $"{referenceNumber}{orderSuffix}",
                            taxable = true
                        };

                        _context.Vtex.Logger.Debug("CapturePayment", "Ecuador custom payload",
                            $"Capture full amount? {captureFullAmount}",
                            new[]
                            {
                            ( "PaymentId", capturePaymentRequest.PaymentId ),
                            ( "payment", JsonConvert.SerializeObject(payment) )
                            });
                    }
                    catch(Exception ex)
                    {
                        _context.Vtex.Logger.Error("CapturePayment", "Ecuador custom payload",
                            "Error building custom payload ", ex,
                            new[]
                            {
                                ( "PaymentId", capturePaymentRequest.PaymentId )
                            });
                    }
                }
                #endregion Custom Payload

                PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessCapture(payment, authId, merchantSettings);
                if (paymentsResponse != null)
                {
                    capturePaymentResponse = new CapturePaymentResponse
                    {
                        PaymentId = capturePaymentRequest.PaymentId,
                        RequestId = capturePaymentRequest.RequestId,
                        Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status,
                        Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message,
                        SettleId = paymentsResponse.Status.Equals("PENDING") ? paymentsResponse.Id : string.Empty
                    };

                    decimal captureAmount = 0m;
                    if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                    {
                        decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.totalAmount, out captureAmount);
                    }
                    else
                    {
                        // Try to get transaction from Cybersource
                        SearchResponse searchResponse = await this.SearchTransaction($"{referenceNumber}{orderSuffix}", merchantSettings);
                        if (searchResponse != null)
                        {
                            foreach (var transactionSummary in searchResponse.Embedded.TransactionSummaries.Where(transactionSummary => transactionSummary.ApplicationInformation.Applications.Exists(ai => ai.Name.Equals(CybersourceConstants.Applications.Capture) && ai.ReasonCode.Equals("100"))))
                            {
                                string captureValueString = transactionSummary.OrderInformation.amountDetails.totalAmount;
                                if (decimal.TryParse(captureValueString, out decimal captureValue) && captureValue == capturePaymentRequest.Value)
                                {
                                    capturePaymentResponse.Code = "100";
                                    capturePaymentResponse.Message = "Transaction retrieved from Cybersource.";
                                    capturePaymentResponse.SettleId = transactionSummary.Id;
                                    captureAmount = captureValue;
                                }
                            }
                        }
                    }
                    
                    if(captureAmount == 0m)
                    {
                        capturePaymentResponse.SettleId = string.Empty;
                    }

                    if(paymentData == null)
                    {
                        paymentData = new PaymentData
                        {
                            PaymentId = capturePaymentRequest.PaymentId,
                            AuthorizationId = capturePaymentRequest.AuthorizationId
                        };
                    }

                    capturePaymentResponse.Value = captureAmount;
                    paymentData.CaptureId = capturePaymentResponse.SettleId;
                    paymentData.Value = capturePaymentResponse.Value;
                    paymentData.TransactionId = capturePaymentResponse.PaymentId;
                    if(string.IsNullOrEmpty(paymentData.CaptureId))
                    {
                        _context.Vtex.Logger.Error("CapturePayment", null,
                        "Empty Capture Id ", null,
                        new[]
                        {
                            ( "PaymentId", capturePaymentRequest.PaymentId )
                        });
                    }

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

        /// <summary>
        /// Refund Payment
        /// </summary>
        /// <param name="refundPaymentRequest"></param>
        /// <returns></returns>
        public async Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResponse refundPaymentResponse = new RefundPaymentResponse();

            try
            {
                PaymentData paymentData = await _cybersourceRepository.GetPaymentData(refundPaymentRequest.PaymentId);
                string referenceNumber = await _vtexApiService.GetOrderId(refundPaymentRequest.PaymentId);
                string orderSuffix = string.Empty;
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                string merchantName = paymentData.CreatePaymentRequest.MerchantName;
                string merchantTaxId = string.Empty;
                bool doCapture = false;
                (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, paymentData.CreatePaymentRequest.MerchantSettings, merchantName);
                if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                {
                    orderSuffix = merchantSettings.OrderSuffix.Trim();
                }

                Payments payment = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = $"{referenceNumber}{orderSuffix}",
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

                if(!string.IsNullOrEmpty(merchantSettings.Processor) && merchantSettings.Processor.Equals(CybersourceConstants.Processors.Banorte))
                {
                    payment.processingInformation = new ProcessingInformation
                    {
                        reconciliationId = paymentData.CaptureId
                    };
                }    

                PaymentsResponse paymentsResponse = await _cybersourceApi.RefundCapture(payment, paymentData.CaptureId, merchantSettings);
                if (paymentsResponse != null)
                {
                    refundPaymentResponse = new RefundPaymentResponse
                    {
                        PaymentId = refundPaymentRequest.PaymentId,
                        RequestId = refundPaymentRequest.RequestId,
                        Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message,
                        RefundId = paymentsResponse.Status.Equals("PENDING") ? paymentsResponse.Id : string.Empty,
                        Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status
                    };

                    if (paymentsResponse.RefundAmountDetails != null && paymentsResponse.RefundAmountDetails.RefundAmount != null)
                    {
                        refundPaymentResponse.Value = decimal.Parse(paymentsResponse.RefundAmountDetails.RefundAmount);
                    }
                    
                    if(refundPaymentResponse.Value == 0m)
                    {
                        refundPaymentResponse.RefundId = string.Empty;
                    }
                }

                if(string.IsNullOrEmpty(refundPaymentResponse.RefundId))
                {
                    _context.Vtex.Logger.Error("RefundPayment", null,
                    "Failed to Refund.", null,
                    new[]
                    {
                        ( "PaymentId", refundPaymentRequest.PaymentId ),
                        ( "Message", refundPaymentResponse.Message ),
                        ( "Code", refundPaymentResponse.Code )
                    });
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

                    PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(sendAntifraudDataRequest);
                    MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                    string merchantName = string.Empty;
                    string merchantTaxId = string.Empty;
                    bool doCapture = false;
                    (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, sendAntifraudDataRequest.MerchantSettings, merchantName);

                    requestWrapper.MerchantId = merchantSettings.MerchantId;
                    requestWrapper.CompanyName = merchantName;
                    requestWrapper.CompanyTaxId = merchantTaxId;
                    payment.merchantDefinedInformation = await this.GetMerchantDefinedInformation(merchantSettings, requestWrapper);

                    PaymentsResponse paymentsResponse = await _cybersourceApi.CreateDecisionManager(payment, merchantSettings);

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

                    if (sendAntifraudDataResponse != null)
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
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(createPaymentRequest.PaymentId);

            if(paymentData != null && paymentData.CreatePaymentResponse != null)
            {
                return paymentData.CreatePaymentResponse;
            }

            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string merchantName = createPaymentRequest.MerchantName;
            (merchantSettings, _, _, _) = await this.ParseGatewaySettings(merchantSettings, createPaymentRequest.MerchantSettings, merchantName);

            Payments payment = await this.BuildPayment(createPaymentRequest, merchantSettings);

            try
            {
                paymentsResponse = await _cybersourceApi.SetupPayerAuth(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl, merchantSettings);
                ConsumerAuthenticationInformationWrapper consumerAuthenticationInformationWrapper = new ConsumerAuthenticationInformationWrapper(paymentsResponse.ConsumerAuthenticationInformation)
                {
                    CreatePaymentRequestReference = createPaymentRequest.PaymentId
                };

                createPaymentResponse = new CreatePaymentResponse
                {
                    PaymentAppData = new PaymentAppData
                    {
                        AppName = CybersourceConstants.PaymentFlowAppName,
                        Payload = JsonConvert.SerializeObject(consumerAuthenticationInformationWrapper)
                    },
                    PaymentId = createPaymentRequest.PaymentId,
                    Status = CybersourceConstants.VtexAuthStatus.Undefined
                };

                paymentData = new PaymentData
                {
                    CreatePaymentRequest = createPaymentRequest,
                    PayerAuthReferenceId = paymentsResponse.ConsumerAuthenticationInformation.ReferenceId,
                    CreatePaymentResponse = createPaymentResponse
                };

                await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetupPayerAuth", null, "Error", ex);
            }

            _context.Vtex.Logger.Debug("SetupPayerAuth", null, string.Empty,
                new[] {
                    ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)),
                    ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse)),
                    ("createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse))
                });

            return createPaymentResponse;
        }

        public async Task<PaymentsResponse> CheckPayerAuthEnrollment(PaymentData paymentData)
        {
            PaymentsResponse paymentsResponse = null;
            if (!string.IsNullOrEmpty(paymentData.PayerAuthReferenceId))
            {
                try
                {
                    MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                    string merchantName = paymentData.CreatePaymentRequest.MerchantName;
                    string merchantTaxId = string.Empty;
                    bool doCapture = false;
                    (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, paymentData.CreatePaymentRequest.MerchantSettings, merchantName);

                    Payments payment = await this.BuildPayment(paymentData.CreatePaymentRequest, merchantSettings);
                    payment.consumerAuthenticationInformation = new ConsumerAuthenticationInformation
                    {
                        ReferenceId = paymentData.PayerAuthReferenceId,
                        TransactionMode = "S",   // S: eCommerce
                        ReturnUrl = $"https://{_context.Vtex.Workspace}--{_context.Vtex.Account}.myvtex.com/cybersource/payer-auth-response"
                    };

                    paymentsResponse = await _cybersourceApi.CheckPayerAuthEnrollment(payment, paymentData.CreatePaymentRequest.SecureProxyUrl, paymentData.CreatePaymentRequest.SecureProxyTokensUrl, merchantSettings);
                    paymentData.ConsumerAuthenticationInformation = paymentsResponse.ConsumerAuthenticationInformation;
                    await _cybersourceRepository.SavePaymentData(paymentData.PaymentId, paymentData);

                    _context.Vtex.Logger.Debug("CheckPayerAuthEnrollment", null, string.Empty, new[]
                    {
                        ("createPaymentRequest", JsonConvert.SerializeObject(paymentData.CreatePaymentRequest)),
                        ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
                    });
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("CheckPayerAuthEnrollment", null, "Error", ex);
                }
            }
            else
            {
                _context.Vtex.Logger.Warn("CheckPayerAuthEnrollment", null, "Missing PayerAuthReferenceId", new[]
                {
                    ("createPaymentRequest", JsonConvert.SerializeObject(paymentData.CreatePaymentRequest)),
                    ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
                });
            }

            return paymentsResponse;
        }

        public async Task<PaymentsResponse> ValidateAuthenticationResults(CreatePaymentRequest createPaymentRequest, string authenticationTransactionId)
        {
            PaymentsResponse paymentsResponse = null;
            
            try
            {
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                string merchantName = createPaymentRequest.MerchantName;
                string merchantTaxId = string.Empty;
                bool doCapture = false;
                (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, createPaymentRequest.MerchantSettings, merchantName);

                Payments payment = await this.BuildPayment(createPaymentRequest, merchantSettings);
                payment.consumerAuthenticationInformation = new ConsumerAuthenticationInformation
                {
                    AuthenticationTransactionId = authenticationTransactionId
                };

                paymentsResponse = await _cybersourceApi.ValidateAuthenticationResults(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl, merchantSettings);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ValidateAuthenticationResults", null, "Error", ex);
            }

            _context.Vtex.Logger.Debug("ValidateAuthenticationResults", null, string.Empty, new[]
            {
                ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)),
                ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
            });

            return paymentsResponse;
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
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            return await _cybersourceApi.RetrieveAvailableReports(dtStartTime, dtEndTime, merchantSettings);
        }

        public async Task<string> RetrieveAvailableReports(string startTime, string endTime)
        {
            DateTime dtStartTime = DateTime.Parse(startTime);
            DateTime dtEndTime = DateTime.Parse(endTime);
            return await this.RetrieveAvailableReports(dtStartTime, dtEndTime);
        }

        public async Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime)
        {
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            return await _cybersourceApi.GetPurchaseAndRefundDetails(dtStartTime, dtEndTime, merchantSettings);
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
            (CreatePaymentResponse createPaymentResponse, _) = await CreatePayment(createPaymentRequest);

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
                        case "EC": // Ecuador
                            regionCode = GetAdministrativeAreaEcuador(region);
                            break;
                        case "PA": // Panama
                            regionCode = GetAdministrativeAreaPanama(region);
                            break;
                        case "BO": // Bolivia
                        case "GT": // Guatemala
                        case "PR": // Puerto Rico
                        case "DO": // Dominican Republic
                        case "CR": // Costa Rica
                        case "SV": // El Salvador
                            regionCode = null;
                            break;
                    }
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
            string regionCode;
            switch (region.ToLowerInvariant())
            {
                case "distrito capital de bogotá":
                case "distrito capital":
                case "bogotá, d.c.":
                case "bogota, d.c.":
                    regionCode = "DC";
                    break;
                case "guaviare":
                    regionCode = "GUV";
                    break;
                case "norte de santander":
                    regionCode = "NSA";
                    break;
                case "san andrés":
                case "san andres":
                case "san andrés, providencia y santa catalina":
                case "san andres, providencia y santa catalina":
                    regionCode = "SAP";
                    break;
                case "valle del cauca":
                case "valle":
                    regionCode = "VAC";
                    break;
                case "vichada":
                    regionCode = "VID";
                    break;
                default:
                    region = region.Replace(" ", string.Empty);
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaPeru(string region)
        {
            string regionCode;
            switch (region.ToLowerInvariant())
            {
                case "ankashu":
                case "anqash":
                    regionCode = "ANC";
                    break;
                case "arikipa":
                case "ariqipa":
                    regionCode = "ARE";
                    break;
                case "el callao":
                case "kallao":
                case "qallaw":
                    regionCode = "CAL";
                    break;
                case "kashamarka":
                case "qajamarka":
                    regionCode = "CAJ";
                    break;
                case "kusku":
                case "qusqu":
                    regionCode = "CUS";
                    break;
                case "huancavelica":
                case "wankawelika":
                case "wankawillka":
                case "wanuku":
                    regionCode = "HUV";
                    break;
                case "hunin":
                    regionCode = "JUN";
                    break;
                case "huánuco":
                case "huanuco":
                    regionCode = "HUC";
                    break;
                case "ika":
                    regionCode = "ICA";
                    break;
                case "la libertad":
                case "qispi kay":
                    regionCode = "LAL";
                    break;
                case "lima hatun llaqta":
                case "lima llaqta suyu":
                case "municipalidad metropolitana de lima":
                    regionCode = "LMA";
                    break;
                case "luritu":
                    regionCode = "LOR";
                    break;
                case "madre de dios":
                case "mayutata":
                    regionCode = "MDD";
                    break;
                case "muqiwa":
                    regionCode = "MOQ";
                    break;
                case "piwra":
                    regionCode = "PIU";
                    break;
                case "san martín":
                case "san martin":
                    regionCode = "SAM";
                    break;
                case "takna":
                case "taqna":
                    regionCode = "TAC";
                    break;
                case "ukayali":
                    regionCode = "UCA";
                    break;
                case "vichada":
                    regionCode = "VID";
                    break;
                default:
                    region = region.Replace(" ", string.Empty);
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaMexico(string region)
        {
            string regionCode;
            switch (region.ToLowerInvariant())
            {
                case "baja california":
                    regionCode = "BCN";
                    break;
                case "baja california sur":
                    regionCode = "BCS";
                    break;
                case "chiapas":
                    regionCode = "CHP";
                    break;
                case "chihuahua":
                    regionCode = "CHH";
                    break;
                case "ciudad de méxico":
                case "ciudad de mexico":
                    regionCode = "CMX";
                    break;
                case "guerrero":
                    regionCode = "GRO";
                    break;
                case "méxico":
                    regionCode = "MEX";
                    break;
                case "nuevo león":
                case "nuevo leon":
                    regionCode = "NLE";
                    break;
                case "quintana roo":
                    regionCode = "ROO";
                    break;
                case "san luis potosí":
                    regionCode = "SLP";
                    break;
                default:
                    region = region.Replace(" ", string.Empty);
                    regionCode = region.Substring(0, 3).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaEcuador(string region)
        {
            string regionCode;
            switch (region.ToLowerInvariant())
            {
                case "cañar":
                case "canar":
                    regionCode = "F";
                    break;
                case "chimborazo":
                    regionCode = "H";
                    break;
                case "cotopaxi":
                    regionCode = "X";
                    break;
                case "el oro":
                    regionCode = "O";
                    break;
                case "galápagos":
                case "galapagos":
                    regionCode = "W";
                    break;
                case "los ríos":
                case "los rios":
                    regionCode = "R";
                    break;
                case "morona-santiago":
                case "morona santiago":
                    regionCode = "S";
                    break;
                case "orellana":
                    regionCode = "D";
                    break;
                case "pastaza":
                    regionCode = "Y";
                    break;
                case "santa elena":
                    regionCode = "SE";
                    break;
                case "santo domingo de los tsáchilas":
                case "santo domingo de los tsachilas":
                    regionCode = "SD";
                    break;
                case "sucumbíos":
                    regionCode = "U";
                    break;
                default:
                    region = region.Replace(" ", string.Empty);
                    regionCode = region.Substring(0, 1).ToUpper();
                    break;
            }

            return regionCode;
        }

        public string GetAdministrativeAreaPanama(string region)
        {
            if (region.Contains('-'))
            {
                string regionCodeTemp = GetLast(region.Split('-'));
                if (!string.IsNullOrEmpty(regionCodeTemp))
                {
                    bool isNumeric = int.TryParse(regionCodeTemp, out int regionCodeParsed);
                    if (isNumeric && regionCodeParsed > 0 && regionCodeParsed < 13)
                    {
                        return regionCodeTemp;
                    }
                }
            }

            string regionCode;
            switch (region.ToLowerInvariant())
            {
                case "bocas del toro":
                    regionCode = "1";
                    break;
                case "chiriquí":
                case "chiriqui":
                    regionCode = "4";
                    break;
                case "coclé":
                case "cocle":
                    regionCode = "2";
                    break;
                case "colón":
                case "colon":
                    regionCode = "3";
                    break;
                case "darién":
                case "darien":
                    regionCode = "5";
                    break;
                case "emberá":
                case "embera":
                    regionCode = "EM";
                    break;
                case "herrera":
                    regionCode = "6";
                    break;
                case "guna yala":
                case "kuna yala":
                    regionCode = "KY";
                    break;
                case "los santos":
                    regionCode = "7";
                    break;
                case "ngäbe buglé":
                case "ngabe bugle":
                    regionCode = "NB";
                    break;
                case "naso tjër di":
                case "naso tjer di":
                    regionCode = "NT";
                    break;
                case "panamá":
                case "panama":
                    regionCode = "8";
                    break;
                case "panamá oeste":
                case "panama oeste":
                    regionCode = "10";
                    break;
                case "veraguas":
                    regionCode = "9";
                    break;
                default:
                    regionCode = region;
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
            if (!string.IsNullOrEmpty(cardTypeText))
            {
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
            }

            return cardType;
        }

        public CybersourceConstants.CardType FindType(string cardNumber)
        {
            if(string.IsNullOrEmpty(cardNumber))
            {
                return CybersourceConstants.CardType.Unknown;
            }

            // Visa:   / ^4(?!38935 | 011 | 51416 | 576)\d{ 12} (?:\d{ 3})?$/
            // Master: / ^5(?!04175 | 067 | 06699)\d{ 15}$/
            // Amex:   / ^3[47]\d{ 13}$/
            // Hiper:  / ^(38 | 60)\d{ 11} (?:\d{ 3})?(?:\d{ 3})?$/
            // Diners: / ^3(?:0[15] |[68]\d)\d{ 11}$/
            // Elo:    / ^[456](?:011 | 38935 | 51416 | 576 | 04175 | 067 | 06699 | 36368 | 36297)\d{ 10} (?:\d{ 2})?$/
            //https://www.regular-expressions.info/creditcard.html
            if (Regex.Match(cardNumber, @"^4[0-9]{5}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.Visa;
            }

            if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{3}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{3}|27[01][0-9]|2720)[0-9]$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.MasterCard;
            }

            if (Regex.Match(cardNumber, @"^3[47][0-9]{4}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.AmericanExpress;
            }

            if (Regex.Match(cardNumber, @"^65[4-9][0-9]{3}|64[4-9][0-9]{3}|6011[0-9]{2}|(622(?:12[6-9]|1[3-9][0-9]|[2-8][0-9][0-9]|9[01][0-9]|92[0-5])[0-9]{3})$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.Discover;
            }

            if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{1}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.JCB;
            }

            if (Regex.Match(cardNumber, @"^3(?:0[0-5]|[68][0-9])[0-9]{3}$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.Diners;
            }

            //if (Regex.Match(cardNumber, @"^(636368|438935|504175|451416|636297|5067[0-9]{2}|4576[0-9]{2}|4011[0-9]{2})$").Success)
            if (Regex.Match(cardNumber, @"^[456](?:011|38935|51416|576|04175|067|06699|36368|36297)?$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
            {
                return CybersourceConstants.CardType.Elo;
            }

            if (Regex.Match(cardNumber, @"^(38|60)\d{4}?$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success)
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
                        string customFieldKey = GetLast(propertyName.Split('.')); //propertyName.Split('.').Last();
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
                                            string valueSubStr = merchantDefinedValue[start..merchantDefinedValue.IndexOf(endCharacter)];
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
                                                                        char padChar = paddArr[1][0];
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
                                                                    propValue = propValue[offset..];
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
                                                                    string timespanFormat = valueSubStrArr[2];
                                                                    if (DateTime.TryParse(propValue, out DateTime propAsDate))
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
                                            sanityCheck++;
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

        public void BinLookup(string cardBin, string paymentMethod, out bool isDebit, out string cardType, out CybersourceConstants.CardType cardBrandName, MerchantSettings merchantSettings)
        {
            isDebit = false;
            VtexBinLookup vtexBinLookup = _vtexApiService.VtexBinLookup(cardBin).Result;
            if (vtexBinLookup != null)
            {
                if (!string.IsNullOrEmpty(vtexBinLookup.CardType))
                {
                    isDebit = vtexBinLookup.CardType.ToLower().Equals("debit");
                }

                cardType = this.GetCardType(vtexBinLookup.CardBrand);
                if (!Enum.TryParse(vtexBinLookup.CardBrand, true, out cardBrandName))
                {
                    cardBrandName = this.FindType(cardBin);
                }
            }
            else
            {
                CybersourceBinLookupResponse cybersourceBinLookup = _cybersourceApi.BinLookup(cardBin, merchantSettings).Result;
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

        public async Task<string> GetReconciliationId(MerchantSettings merchantSettings, CreatePaymentRequest createPaymentRequest)
        {
            string reconciliationId = createPaymentRequest.Reference;
            try
            {
                if (!string.IsNullOrWhiteSpace(merchantSettings.CustomNsu))
                {
                    MerchantSettings nsuSettings = new MerchantSettings
                    {
                        MerchantDefinedValueSettings = new List<MerchantDefinedValueSetting>
                                {
                                    new MerchantDefinedValueSetting
                                    {
                                        GoodPortion = merchantSettings.CustomNsu,
                                        IsValid = true,
                                        UserInput = merchantSettings.CustomNsu
                                    }
                                }
                    };

                    PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(createPaymentRequest);
                    List<MerchantDefinedInformation> merchantDefinedNsu = await this.GetMerchantDefinedInformation(nsuSettings, requestWrapper);
                    string customNsu = merchantDefinedNsu[0].value;
                    if (!string.IsNullOrWhiteSpace(customNsu))
                    {
                        reconciliationId = customNsu;
                    }
                }
            }
            catch(Exception ex)
            {
                _context.Vtex.Logger.Error("GetReconciliationId", null,
                "Error: ", ex,
                new[]
                {
                    ( "merchantSettings", JsonConvert.SerializeObject(merchantSettings) ),
                    ( "createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest) )
                });
            }

            return reconciliationId;
        }

        public async Task<RetrieveTransaction> RetrieveTransaction(string requestId)
        {
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            RetrieveTransaction retrieveTransaction = await _cybersourceApi.RetrieveTransaction(requestId, merchantSettings);
            return retrieveTransaction;
        }

        public async Task<SearchResponse> SearchTransaction(string referenceNumber, MerchantSettings merchantSettings)
        {
            CreateSearchRequest searchRequest = new CreateSearchRequest
            {
                Query = $"clientReferenceInformation.code:{referenceNumber}",
                Sort = "submitTimeUtc:desc",
                Limit = 2000
            };

            SearchResponse searchResponse = await _cybersourceApi.CreateSearchRequest(searchRequest, merchantSettings);
            return searchResponse;
        }

        public async Task<(CreatePaymentResponse createPaymentResponse, PaymentsResponse paymentsResponse, string paymentStatus, bool doCancel)> GetPaymentStatus(CreatePaymentResponse createPaymentResponse, CreatePaymentRequest createPaymentRequest, PaymentsResponse paymentsResponse, bool isPayerAuth)
        {
            string paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
            bool doCancel = false;
            if (paymentsResponse != null && paymentsResponse.Status != null)
            {
                // AUTHORIZED
                // PARTIAL_AUTHORIZED
                // AUTHORIZED_PENDING_REVIEW
                // AUTHORIZED_RISK_DECLINED
                // PENDING_AUTHENTICATION
                // PENDING_REVIEW
                // DECLINED
                // INVALID_REQUEST
                try
                {
                    switch (paymentsResponse.Status)
                    {
                        case "AUTHORIZED":
                        case "PARTIAL_AUTHORIZED":
                        case "AUTHENTICATION_SUCCESSFUL":
                        case "SOK":
                        case "ACCEPTED":
                            if (isPayerAuth)
                            {
                                if (!string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.VeresEnrolled) &&
                                    !paymentsResponse.ConsumerAuthenticationInformation.VeresEnrolled.Equals("Y"))
                                {
                                    //Y - Authentication Successful
                                    //U - Authentication Could Not Be Performed
                                    //B - Bypassed Authentication
                                    //N – Cardholder not participating
                                    paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                                    doCancel = true;
                                    paymentsResponse.Status = "REFUSED";
                                    break;
                                }

                                // MC
                                if ((paymentsResponse.PaymentInformation.card.type.Equals("002") &&
                                    string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.UcafCollectionIndicator)) ||
                                    (!string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.UcafCollectionIndicator) &&
                                    paymentsResponse.ConsumerAuthenticationInformation.UcafCollectionIndicator.Equals("0")))
                                {
                                    paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                                    doCancel = true;
                                    paymentsResponse.Status = "REFUSED";
                                    break;
                                }

                                // Visa or Amex
                                if ((paymentsResponse.PaymentInformation.card.type.Equals("001") || paymentsResponse.PaymentInformation.card.type.Equals("003")) &&
                                    (string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.Eci)) ||
                                    (!string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.Eci) &&
                                    (paymentsResponse.ConsumerAuthenticationInformation.Eci.Equals("00") ||
                                    paymentsResponse.ConsumerAuthenticationInformation.Eci.Equals("07"))))
                                {
                                    //05 - Authentication successful para Visa / Amex
                                    //02 - Authentication successful para Mastercard
                                    //06 - Authentication attempted para Visa / Amex
                                    //01 - Authentication attempted para Mastercard
                                    //07 - Authentication failed para Visa / Amex
                                    //00 - Authentication failed para Mastercard
                                    paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                                    doCancel = true;
                                    paymentsResponse.Status = "REFUSED";
                                    break;
                                }
                            }

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
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
                            createPaymentResponse.DelayToCancel = 5 * 60 * 60 * 24;
                            bool isDecisionManagerInUse = true;
                            try
                            {
                                MerchantSetting merchantSettingDecisionManagerInUse = createPaymentRequest.MerchantSettings.Find(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.DecisionManagerInUse));
                                if (merchantSettingDecisionManagerInUse != null && merchantSettingDecisionManagerInUse.Value != null && merchantSettingDecisionManagerInUse.Value.Equals(CybersourceConstants.ManifestCustomField.Disabled, StringComparison.OrdinalIgnoreCase))
                                {
                                    isDecisionManagerInUse = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                paymentStatus = CybersourceConstants.VtexAuthStatus.Approved; // jic
                                _context.Vtex.Logger.Error("GetPaymentStatus", "Decision Manager Active Setting",
                                "Error: ", ex,
                                new[]
                                {
                                    ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest))
                                });
                            }

                            if(!isDecisionManagerInUse)
                            {
                                // If Decision Manager is not used, mark Pending as Approved
                                paymentStatus = CybersourceConstants.VtexAuthStatus.Approved;
                            }

                            break;
                        case "PENDING_AUTHENTICATION":
                        case "PENDING_REVIEW":
                        case "INVALID_REQUEST":
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
                            createPaymentResponse.DelayToCancel = 5 * 60 * 60 * 24;
                            break;
                        case "DECLINED":
                        case "CONSUMER_AUTHENTICATION_FAILED":
                        case "AUTHENTICATION_FAILED":
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                            break;
                        case "AUTHORIZED_RISK_DECLINED":
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                            try
                            {
                                MerchantSetting merchantSettingRiskDeclined = null;
                                if (createPaymentRequest.MerchantSettings != null)
                                {
                                    merchantSettingRiskDeclined = createPaymentRequest.MerchantSettings.Find(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.AuthorizedRiskDeclined));
                                    if (merchantSettingRiskDeclined != null && merchantSettingRiskDeclined.Value != null)
                                    {
                                        paymentStatus = merchantSettingRiskDeclined.Value.ToLower();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                paymentStatus = CybersourceConstants.VtexAuthStatus.Denied; // jic
                                _context.Vtex.Logger.Error("GetPaymentStatus", "Risk Declined Setting",
                                "Error: ", ex,
                                new[]
                                {
                                    ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest))
                                });
                            }

                            break;
                        case "ERROR":
                            string referenceNumber = await _vtexApiService.GetOrderId(createPaymentRequest.Reference, createPaymentRequest.OrderId);
                            await Task.Delay(6000); // wait for transaction to be available
                            string orderSuffix = string.Empty;
                            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                            string merchantName = createPaymentRequest.MerchantName;
                            string merchantTaxId = string.Empty;
                            bool doCapture = false;
                            (merchantSettings, merchantName, merchantTaxId, doCapture) = await this.ParseGatewaySettings(merchantSettings, createPaymentRequest.MerchantSettings, merchantName);
                            if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix))
                            {
                                orderSuffix = merchantSettings.OrderSuffix.Trim();
                            }

                            SearchResponse searchResponse = await this.SearchTransaction($"{referenceNumber}{orderSuffix}", merchantSettings);
                            if (searchResponse != null)
                            {
                                _context.Vtex.Logger.Warn("GetPaymentStatus", null, "Loaded Transactions from Cybersource", new[] { ("searchResponse", JsonConvert.SerializeObject(searchResponse)) });
                                // First transaction should be the most recent
                                TransactionSummary transactionSummary = searchResponse.Embedded.TransactionSummaries[0];
                                string authValueString = string.Empty;
                                string captureValueString = string.Empty;
                                foreach (Application application in transactionSummary.ApplicationInformation.Applications)
                                {
                                    if (application.Name.Equals(CybersourceConstants.Applications.Authorize) && application.ReasonCode.Equals("100"))
                                    {
                                        authValueString = transactionSummary.OrderInformation.amountDetails.totalAmount;
                                    }

                                    if (application.Name.Equals(CybersourceConstants.Applications.Capture) && application.ReasonCode.Equals("100"))
                                    {
                                        captureValueString = transactionSummary.OrderInformation.amountDetails.totalAmount;
                                    }
                                }

                                paymentsResponse = new PaymentsResponse
                                {
                                    ConsumerAuthenticationInformation = DeepCopy<ConsumerAuthenticationInformation>(transactionSummary.ConsumerAuthenticationInformation),
                                    ClientReferenceInformation = DeepCopy<ClientReferenceInformation>(transactionSummary.ClientReferenceInformation),
                                    Id = transactionSummary.Id,
                                    ReconciliationId = transactionSummary.ApplicationInformation.Applications[0].ReconciliationId,
                                    OrderInformation = DeepCopy<OrderInformation>(transactionSummary.OrderInformation),
                                    ProcessorInformation = DeepCopy<ProcessorInformation>(transactionSummary.ProcessorInformation),
                                    Status = transactionSummary.ApplicationInformation.RFlag,
                                    SubmitTimeUtc = transactionSummary.SubmitTimeUtc,
                                    Message = "Transaction(s) retrieved from Cybersource.",
                                    PaymentInformation = DeepCopy<PaymentInformation>(transactionSummary.PaymentInformation)
                                };

                                paymentsResponse.OrderInformation.amountDetails.authorizedAmount = authValueString;
                                paymentsResponse.OrderInformation.amountDetails.totalAmount = captureValueString;
                                if (paymentsResponse.ProcessorInformation != null)
                                {
                                    paymentsResponse.ProcessorInformation.TransactionId = transactionSummary.Id;
                                }

                                if (paymentsResponse.ConsumerAuthenticationInformation != null && string.IsNullOrEmpty(paymentsResponse.ConsumerAuthenticationInformation.Eci))
                                {
                                    paymentsResponse.ConsumerAuthenticationInformation.Eci = paymentsResponse.ConsumerAuthenticationInformation.EciRaw.PadLeft(2, '0');
                                }

                                if (!paymentsResponse.Status.Equals("ERROR"))
                                {
                                    // If status is not ERROR (to prevent an infinate loop) re-run this function to apply Payer Auth and Anti-fraud logic
                                    (createPaymentResponse, paymentsResponse, paymentStatus, doCancel) = await this.GetPaymentStatus(createPaymentResponse, createPaymentRequest, paymentsResponse, isPayerAuth);
                                }
                                else
                                {
                                    paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                                }
                            }
                            break;
                        default:
                            paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                            _context.Vtex.Logger.Warn("CreatePayment", null, $"Invalid Status: {paymentsResponse.Status}", new[]
                            {
                                ("OrderId", createPaymentRequest.OrderId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)),
                                ("createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse)),
                                ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    paymentStatus = CybersourceConstants.VtexAuthStatus.Denied;
                    _context.Vtex.Logger.Error("CreatePayment", null, $"Invalid Status: {paymentsResponse.Status}", ex, new[]
                            {
                                ("OrderId", createPaymentRequest.OrderId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)),
                                ("createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse)),
                                ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
                            });
                }

                createPaymentResponse.Status = paymentStatus;
            }
            else
            {
                _context.Vtex.Logger.Error("CreatePayment", null, $"Null Payments Response.");
            }

            return (createPaymentResponse, paymentsResponse, paymentStatus, doCancel);
        }

        public void GetItemTaxAmounts(MerchantSettings merchantSettings, List<VtexOrderItem> vtexOrderItems, Payments payment, CreatePaymentRequest createPaymentRequest)
        {
            decimal taxRate = 0M;
            bool useRate = false;
            double shippingTaxAmount = 0D;
            decimal taxDetailAmount = 0M;
            double totalItemTax = 0D;
            foreach (VtexItem vtexItem in createPaymentRequest.MiniCart.Items)
            {
                string taxAmount = "0.00";
                string commodityCode = string.Empty;
                long itemTax = 0L;
                double lineItemTax = 0D;
                VtexOrderItem vtexOrderItem = vtexOrderItems.Find(i => (i.Id.Equals(vtexItem.Id)) && (i.Quantity.Equals(vtexItem.Quantity)));
                if (vtexOrderItem != null)
                {
                    foreach (PriceTag priceTag in vtexOrderItem.PriceTags)
                    {
                        string name = priceTag.Name.ToLower();
                        if (name.Contains("tax@") || name.Contains("taxhub@"))
                        {
                            if (name.Contains("shipping"))
                            {
                                if (priceTag.IsPercentual ?? false)
                                {
                                    taxRate = (decimal)priceTag.RawValue;
                                    useRate = true;
                                }
                                else
                                {
                                    shippingTaxAmount += priceTag.RawValue;
                                }
                            }
                            else
                            {
                                if (priceTag.IsPercentual ?? false)
                                {
                                    itemTax += (long)Math.Round(vtexOrderItem.SellingPrice * priceTag.RawValue, MidpointRounding.AwayFromZero);
                                    lineItemTax += vtexOrderItem.SellingPrice * vtexItem.Quantity * priceTag.RawValue;
                                }
                                else
                                {
                                    itemTax += priceTag.Value / vtexOrderItem.Quantity;
                                    lineItemTax += priceTag.Value;
                                }
                            }
                        }
                    }

                    taxAmount = Math.Round((decimal)itemTax / 100, MidpointRounding.AwayFromZero).ToString("0.00");
                    commodityCode = vtexOrderItem.TaxCode;
                }

                try
                {
                    LineItem lineItem = new LineItem
                    {
                        productSKU = vtexItem.Id,
                        productName = vtexItem.Name,
                        unitPrice = (vtexItem.Price + (vtexItem.Discount / vtexItem.Quantity)).ToString("0.00"), // Discount is negative
                        quantity = vtexItem.Quantity.ToString(),
                        discountAmount = vtexItem.Discount.ToString("0.00"),
                        taxAmount = taxAmount,
                        commodityCode = commodityCode
                    };

                    if (merchantSettings.Region != null && merchantSettings.Region.Equals(CybersourceConstants.Regions.Ecuador))
                    {
                        decimal unitPrice = (vtexItem.Price + (vtexItem.Discount / vtexItem.Quantity));
                        lineItem.taxAmount = itemTax > 0 ? (unitPrice * vtexItem.Quantity).ToString("0.00") : "0.00";
                        lineItem.taxDetails = new TaxDetail[]
                        {
                                new TaxDetail
                                {
                                    type = "national",
                                    amount = Math.Round(lineItemTax / 100d, 2, MidpointRounding.AwayFromZero).ToString("0.00")
                                }
                        };
                    }

                    payment.orderInformation.lineItems.Add(lineItem);
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("GetItemTaxAmounts", "LineItems", "Error", ex);
                }

                totalItemTax += lineItemTax;
            }

            try
            {
                if (useRate)
                {
                    taxDetailAmount = createPaymentRequest.MiniCart.ShippingValue * taxRate;
                }
                else
                {
                    taxDetailAmount = (decimal)shippingTaxAmount;
                }

                if (merchantSettings.Region != null && merchantSettings.Region.Equals(CybersourceConstants.Regions.Ecuador))
                {
                    // Add shipping tax as a line item
                    payment.orderInformation.amountDetails.nationalTaxIncluded = createPaymentRequest.MiniCart.TaxValue > 0m ? "1" : "0";
                    payment.orderInformation.invoiceDetails = new InvoiceDetails
                    {
                        purchaseOrderNumber = createPaymentRequest.OrderId,
                        taxable = createPaymentRequest.MiniCart.TaxValue > 0m
                    };

                    LineItem lineItem = new LineItem
                    {
                        productName = "ADMINISTRACION MANEJO DE PRODUCTO",
                        unitPrice = createPaymentRequest.MiniCart.ShippingValue.ToString("0.00"), // Shipping cost without taxes
                        quantity = "1",
                        taxAmount = createPaymentRequest.MiniCart.ShippingValue.ToString("0.00"), // unitPrice * quantity
                        taxDetails = new TaxDetail[]
                        {
                                new TaxDetail
                                {
                                    type = "national",
                                    amount = Math.Round(taxDetailAmount, 2, MidpointRounding.AwayFromZero).ToString()   // taxAmount * taxRate (based on configuration in the account)
                                }
                        }
                    };

                    payment.orderInformation.lineItems.Add(lineItem);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetItemTaxAmounts", "Ecuador Customization", "Error", ex);
            }

            try
            {
                decimal totalOrderTax = createPaymentRequest.MiniCart.TaxValue;
                decimal totalItemTaxAsDecimal = Math.Round((decimal)totalItemTax / 100M, 2, MidpointRounding.AwayFromZero);
                decimal taxDiff = totalOrderTax - totalItemTaxAsDecimal - taxDetailAmount;
                if (taxDiff > 0M)
                {
                    _context.Vtex.Logger.Warn("GetItemTaxAmounts", "Tax Total Verification", $"Modfying tax amount of item '{payment.orderInformation.lineItems[0].productName}' by '{taxDiff}' ");
                    if (merchantSettings.Region != null && merchantSettings.Region.Equals(CybersourceConstants.Regions.Ecuador))
                    {
                        payment.orderInformation.lineItems[0].taxDetails[0].amount = (decimal.Parse(payment.orderInformation.lineItems[0].taxDetails[0].amount) + taxDiff).ToString("0.00");
                    }
                    else
                    {
                        payment.orderInformation.lineItems[0].taxAmount = (decimal.Parse(payment.orderInformation.lineItems[0].taxAmount) + taxDiff).ToString("0.00");
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetItemTaxAmounts", "Tax Total Verification", "Error", ex);
            }
        }

        /// <summary>
        /// Parse settings from the Gateway
        /// </summary>
        /// <param name="merchantSettings">Merchant Settings from Admin page</param>
        /// <param name="gatewaySettings">Merchant Settings from Gateway Settings</param>
        /// <param name="merchantName">Default Merchant Name</param>
        /// <returns>
        /// <param name="merchantSettings">Merchant Settings with Gateway override</param>
        /// <param name="merchantName">merchantName</param>
        /// <param name="merchantTaxId">merchantTaxId</param>
        /// <param name="doCapture">doCapture</param>
        /// </returns>
        public async Task<(MerchantSettings, string, string, bool)> ParseGatewaySettings(MerchantSettings merchantSettings, List<MerchantSetting> gatewaySettings, string merchantName)
        {
            string merchantTaxId = string.Empty;
            bool doCapture = false;

            if (gatewaySettings != null)
            {
                foreach (MerchantSetting merchantSetting in gatewaySettings)
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
                            if (!string.IsNullOrWhiteSpace(merchantSetting.Value))
                            {
                                merchantSettings.MerchantId = merchantSetting.Value;
                            }

                            break;
                        case CybersourceConstants.ManifestCustomField.MerchantKey:
                            if (!string.IsNullOrWhiteSpace(merchantSetting.Value))
                            {
                                merchantSettings.MerchantKey = merchantSetting.Value;
                            }

                            break;
                        case CybersourceConstants.ManifestCustomField.SharedSecretKey:
                            if (!string.IsNullOrWhiteSpace(merchantSetting.Value))
                            {
                                merchantSettings.SharedSecretKey = merchantSetting.Value;
                            }

                            break;
                    }
                }
            }

            return (merchantSettings, merchantName, merchantTaxId, doCapture);
        }

        private static T DeepCopy<T>(object objToCopy)
        {
            if (objToCopy != null)
            {
                string objAsString = JsonConvert.SerializeObject(objToCopy);
                return JsonConvert.DeserializeObject<T>(objAsString);
            }
            else
            {
                return default;
            }
        }

        private string GetLast(IList<string> data) => data[data.Count - 1];
    }
}
