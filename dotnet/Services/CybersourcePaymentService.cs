using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cybersource.Data;
using Cybersource.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
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
        private readonly string _applicationName;

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

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        #region Payments
        public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest)
        {
            CreatePaymentResponse createPaymentResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(createPaymentRequest.PaymentId);
            if(paymentData != null && paymentData.CreatePaymentResponse != null)
            {
                _context.Vtex.Logger.Debug("CreatePayment", null, "Loaded PaymentData", new[] { ("orderId", createPaymentRequest.OrderId), ("paymentId", createPaymentRequest.PaymentId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)), ("paymentData", JsonConvert.SerializeObject(paymentData)) });
                await _vtexApiService.ProcessConversions();
                return paymentData.CreatePaymentResponse;
            }

            _context.Vtex.Logger.Debug("CreatePayment", null, "Creating Payment", new[] { ("orderId", createPaymentRequest.OrderId), ("paymentId", createPaymentRequest.PaymentId), ("createPaymentRequest", JsonConvert.SerializeObject(createPaymentRequest)) });
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string merchantName = createPaymentRequest.MerchantName;
            string merchantTaxId = string.Empty;
            if (createPaymentRequest.MerchantSettings != null)
            {
                foreach (MerchantSetting merchantSetting in createPaymentRequest.MerchantSettings)
                {
                    switch(merchantSetting.Name)
                    {
                        case "Company Name":
                            merchantName = merchantSetting.Value;
                            break;
                        case "Company Tax Id":
                            merchantTaxId = merchantSetting.Value;
                            break;
                    }
                }
            }

            CybersourceConstants.CardType cardBrandName = CybersourceConstants.CardType.Unknown;
            string cardType = string.Empty;
            bool isDebit = false;
            CybersourceBinLookupResponse cybersourceBinLookup = await _cybersourceApi.BinLookup(createPaymentRequest.Card.Bin);
            if (cybersourceBinLookup != null)
            {
                cardType = cybersourceBinLookup.PaymentAccountInformation.Card.Type;
                if (!Enum.TryParse(cybersourceBinLookup.PaymentAccountInformation.Card.BrandName, true, out cardBrandName))
                {
                    cardBrandName = this.FindType(createPaymentRequest.Card.Bin);
                }
            }
            else
            {
                cardType = this.GetCardType(createPaymentRequest.PaymentMethod);
                cardBrandName = this.FindType(createPaymentRequest.Card.Bin);
            }

            Payments payment = new Payments
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
                    applicationUser = _context.Vtex.Account
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
                        locality = createPaymentRequest.MiniCart.BillingAddress.City,
                        administrativeArea = createPaymentRequest.MiniCart.BillingAddress.State,
                        postalCode = createPaymentRequest.MiniCart.BillingAddress.PostalCode,
                        country = this.GetCountryCode(createPaymentRequest.MiniCart.BillingAddress.Country),
                        email = createPaymentRequest.MiniCart.Buyer.Email,
                        phoneNumber = createPaymentRequest.MiniCart.Buyer.Phone
                    },
                    shipTo = new ShipTo
                    {
                        address1 = $"{createPaymentRequest.MiniCart.ShippingAddress.Number} {createPaymentRequest.MiniCart.ShippingAddress.Street}",
                        address2 = createPaymentRequest.MiniCart.ShippingAddress.Complement,
                        administrativeArea = createPaymentRequest.MiniCart.ShippingAddress.State,
                        country = this.GetCountryCode(createPaymentRequest.MiniCart.ShippingAddress.Country),
                        postalCode = createPaymentRequest.MiniCart.ShippingAddress.PostalCode
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

            payment.merchantDefinedInformation = await this.GetMerchantDefinedInformation(merchantSettings, requestWrapper);

            string numberOfInstallments = createPaymentRequest.Installments.ToString("00");
            string plan = string.Empty;
            decimal installmentsInterestRate = createPaymentRequest.InstallmentsInterestRate;

            switch (merchantSettings.Processor)
            {
                case CybersourceConstants.Processors.Braspag:
                    if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Colombia))
                    {
                        payment.processingInformation = new ProcessingInformation
                        {
                            commerceIndicator = CybersourceConstants.INSTALLMENT
                        };

                        payment.installmentInformation = new InstallmentInformation
                        {
                            totalCount = numberOfInstallments
                        };
                    }
                    else if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Mexico))
                    {
                        payment.processingInformation = new ProcessingInformation
                        {
                            capture = "true",
                            commerceIndicator = CybersourceConstants.INSTALLMENT,
                            reconciliationId = createPaymentRequest.PaymentId
                        };

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
                    if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Colombia))
                    {
                        if (cardBrandName.Equals(CybersourceConstants.CardType.Visa) && !isDebit)
                        {
                            payment.processingInformation = new ProcessingInformation
                            {
                                commerceIndicator = CybersourceConstants.INSTALLMENT
                            };

                            payment.installmentInformation = new InstallmentInformation
                            {
                                totalCount = numberOfInstallments
                            };
                        }
                    }

                    break;
                case CybersourceConstants.Processors.Izipay:
                    if (merchantSettings.Region.Equals(CybersourceConstants.Regions.Peru))
                    {
                        if (cardBrandName.Equals(CybersourceConstants.CardType.Visa) || cardBrandName.Equals(CybersourceConstants.CardType.MasterCard))
                        {
                            plan = "0";  // 0: no deferred payment, 1: 30 d�as, 2: 60 d�as, 3: 90 d�as
                            payment.issuerInformation = new IssuerInformation
                            {
                                //POS 1 - 6:014001
                                //POS 7 - 8: # of installments
                                //POS 9 - 16:00000000
                                //POS 17: plan(0: no deferred payment, 1: 30 d�as, 2: 60 d�as, 3: 90 d�as)
                                discretionaryData = $"14001{numberOfInstallments}00000000{plan}"
                            };
                        }
                    }

                    break;
                case CybersourceConstants.Processors.eGlobal:
                case CybersourceConstants.Processors.BBVA:
                    plan = installmentsInterestRate > 0 ? "05" : "03";  // 03 no interest 05 with interest
                    payment.processingInformation = new ProcessingInformation
                    {
                        capture = "true",
                        commerceIndicator = CybersourceConstants.INSTALLMENT_INTERNET
                    };

                    //POS 1 - 2: # of months of deferred payment
                    //POS 3 - 4: # installments
                    //POS 5 - 6: plan(03 no interest, 05 with interest)"
                    string monthsDeferred = "00";
                    payment.installmentInformation = new InstallmentInformation
                    {
                        amount = $"{monthsDeferred}{numberOfInstallments}{plan}",
                        totalCount = numberOfInstallments
                    };

                    break;
                case CybersourceConstants.Processors.Banorte:
                case CybersourceConstants.Processors.AmexDirect:
                    payment.processingInformation = new ProcessingInformation
                    {
                        capture = "true",
                        commerceIndicator = CybersourceConstants.INSTALLMENT,
                        reconciliationId = createPaymentRequest.PaymentId
                    };

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
                    payment.processingInformation = new ProcessingInformation
                    {
                        capture = "true",
                        commerceIndicator = createPaymentRequest.Installments > 1 ? CybersourceConstants.INSTALLMENT : CybersourceConstants.INTERNET,
                        reconciliationId = createPaymentRequest.PaymentId
                    };

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
                    foreach (VtexOrderItem vtexItem in vtexOrder.Items)
                    {
                        if (!vtexOrderItems.Contains(vtexItem))
                        {
                            vtexOrderItems.Add(vtexItem);
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

            PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessPayment(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl);
            if (paymentsResponse != null)
            {
                createPaymentResponse = new CreatePaymentResponse();
                createPaymentResponse.AuthorizationId = paymentsResponse.Id;
                createPaymentResponse.Tid = paymentsResponse.Id;
                createPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message ?? paymentsResponse.Status;
                createPaymentResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Reason : paymentsResponse.Status;
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
                        if(antifraudDataResponse != null)
                        {
                            switch (antifraudDataResponse.Status)
                            {
                                case "ACCEPTED":
                                    createPaymentResponse.Status = CybersourceConstants.VtexAntifraudStatus.Approved;
                                    break;
                                case "PENDING_REVIEW":
                                case "PENDING_AUTHENTICATION":
                                case "INVALID_REQUEST":
                                case "CHALLENGE":
                                    createPaymentResponse.Status = CybersourceConstants.VtexAntifraudStatus.Undefined;
                                    createPaymentResponse.DelayToCancel = 5 * 60 * 60 * 24;
                                    break;
                                case "REJECTED":
                                case "DECLINED":
                                case "AUTHENTICATION_FAILED":
                                    createPaymentResponse.Status = CybersourceConstants.VtexAntifraudStatus.Denied;
                                    break;
                                default:
                                    createPaymentResponse.Status = CybersourceConstants.VtexAntifraudStatus.Undefined;
                                    break;
                            }
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
                if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                {
                    decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.authorizedAmount, out authAmount);
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

                await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
            }

            //_context.Vtex.Logger.Debug("createPaymentResponse", null, JsonConvert.SerializeObject(createPaymentResponse));

            return createPaymentResponse;
        }

        public async Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest cancelPaymentRequest)
        {
            CancelPaymentResponse cancelPaymentResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(cancelPaymentRequest.PaymentId);
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = await _vtexApiService.GetOrderId(cancelPaymentRequest.PaymentId),
                    applicationName = _context.Vtex.App.Name,
                    applicationVersion = _context.Vtex.App.Version,
                    applicationUser = _context.Vtex.App.Vendor
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

            return cancelPaymentResponse;
        }

        public async Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest)
        {
            CapturePaymentResponse capturePaymentResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(capturePaymentRequest.PaymentId);
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = await _vtexApiService.GetOrderId(paymentData.OrderId), //capturePaymentRequest.PaymentId,
                    applicationName = _context.Vtex.App.Name,
                    applicationVersion = _context.Vtex.App.Version,
                    applicationUser = _context.Vtex.App.Vendor
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
                capturePaymentResponse.Value = paymentsResponse.ErrorInformation != null ? 0m : decimal.Parse(paymentsResponse.OrderInformation.amountDetails.totalAmount);
                capturePaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;

                decimal authAmount = 0m;
                if (paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                {
                    decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.authorizedAmount, out authAmount);
                }

                paymentData.CaptureId = capturePaymentResponse.SettleId;
                paymentData.Value = capturePaymentResponse.Value;

                await _cybersourceRepository.SavePaymentData(capturePaymentRequest.PaymentId, paymentData);
            }

            return capturePaymentResponse;
        }

        public async Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest)
        {
            RefundPaymentResponse refundPaymentResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(refundPaymentRequest.PaymentId);
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = await _vtexApiService.GetOrderId(refundPaymentRequest.PaymentId),
                    applicationName = _context.Vtex.App.Name,
                    applicationVersion = _context.Vtex.App.Version,
                    applicationUser = _context.Vtex.App.Vendor
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

            return refundPaymentResponse;
        }
        #endregion Payments

        #region Antifraud
        public async Task<SendAntifraudDataResponse> SendAntifraudData(SendAntifraudDataRequest sendAntifraudDataRequest)
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;
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
                        applicationUser = _context.Vtex.App.Vendor
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
                            locality = sendAntifraudDataRequest.MiniCart.Buyer.Address.City,
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
                };

                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(sendAntifraudDataRequest);
                requestWrapper.MerchantId = merchantSettings.MerchantId;
                string merchantName = string.Empty;
                string merchantTaxId = string.Empty;
                if (sendAntifraudDataRequest.MerchantSettings != null)
                {
                    foreach (MerchantSetting merchantSetting in sendAntifraudDataRequest.MerchantSettings)
                    {
                        switch (merchantSetting.Name)
                        {
                            case "Company Name":
                                merchantName = merchantSetting.Value;
                                break;
                            case "Company Tax Id":
                                merchantTaxId = merchantSetting.Value;
                                break;
                        }
                    }
                }

                requestWrapper.CompanyName = merchantName;
                requestWrapper.CompanyTaxId = merchantTaxId;
                payment.merchantDefinedInformation = await this.GetMerchantDefinedInformation(merchantSettings, requestWrapper);

                PaymentsResponse paymentsResponse = await _cybersourceApi.CreateDecisionManager(payment);

                sendAntifraudDataResponse = new SendAntifraudDataResponse
                {
                    Id = sendAntifraudDataRequest.Id,
                    Tid = paymentsResponse.Id,
                    Status = CybersourceConstants.VtexAntifraudStatus.Undefined,
                    Score = paymentsResponse.RiskInformation != null ? double.Parse(paymentsResponse.RiskInformation.Score.Result) : 100d,
                    AnalysisType = CybersourceConstants.VtexAntifraudType.Automatic,
                    Responses = new Dictionary<string, string>(),
                    Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status,
                    Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message
                };

                switch (paymentsResponse.Status)
                {
                    case "ACCEPTED":
                    case "PENDING_REVIEW":
                    case "PENDING_AUTHENTICATION":
                        sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Approved;   // Set Review to Arroved otherwise auth will not be called
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

                await _cybersourceRepository.SaveAntifraudData(sendAntifraudDataRequest.Id, sendAntifraudDataResponse);
            }
            else
            {
                _context.Vtex.Logger.Warn("SendAntifraudData", null, "Null request");
            }

            return sendAntifraudDataResponse;
        }

        public async Task<SendAntifraudDataResponse> GetAntifraudStatus(string id)
        {
            return await _cybersourceRepository.GetAntifraudData(id);
        }
        #endregion Antifraud

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
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();

            try
            {
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
                _context.Vtex.Logger.Error("GetAuthUrl", null, $"Error getting auth url", ex);
            }

            return authUrl;
        }
        #endregion OAuth

        public async Task<bool> HealthCheck()
        {
            bool success = true;
            success &= await TestPayment();
            success &= await TestAntifraud();
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

        public string GetCountryCode(string country)
        {
            return CybersourceConstants.CountryCodesMapping[country];
        }

        private string GetCardType(string cardTypeText)
        {
            string cardType = null;
            switch(cardTypeText.ToLower())
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
                    obj = prop.GetValue(obj, null);
            }
            catch(Exception ex)
            {
                _context.Vtex.Logger.Error("GetPropertyValue", null, $"Could not get value of '{propertyName}'", ex);
            }

            return obj;
        }

        private async Task<List<MerchantDefinedInformation>> GetMerchantDefinedInformation(MerchantSettings merchantSettings, PaymentRequestWrapper requestWrapper)
        {
            List<MerchantDefinedInformation> merchantDefinedInformationList = new List<MerchantDefinedInformation>();

            string startCharacter = "{{";
            string endCharacter = "}}";
            string valueSeparator = "|";
            try
            {
                if (merchantSettings.MerchantDefinedValueSettings != null)
                {
                    int merchantDefinedValueKey = 0;
                    foreach (MerchantDefinedValueSetting merchantDefinedValueSetting in merchantSettings.MerchantDefinedValueSettings)
                    {
                        merchantDefinedValueKey++;
                        if (merchantDefinedValueSetting.IsValid)
                        {
                            string merchantDefinedValue = merchantDefinedValueSetting.UserInput; // merchantDefinedValueSetting.GoodPortion;
                            if (!string.IsNullOrEmpty(merchantDefinedValue))
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
                                                            DateTime dt = DateTime.Now;
                                                            string dateFormat = valueSubStrArr[2];
                                                            propValue = dt.ToString(dateFormat);
                                                            break;
                                                        default:
                                                            _context.Vtex.Logger.Warn("GetMerchantDefinedInformation", null, $"Invalid operation '{operation}'");
                                                            break;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                propValue = this.GetPropertyValue(requestWrapper, valueSubStr).ToString();
                                            }
                                        }

                                        merchantDefinedValue = merchantDefinedValue.Replace($"{startCharacter}{originalValueSubStr}{endCharacter}", propValue);
                                        sanityCheck = sanityCheck + 1;
                                    }
                                    while (merchantDefinedValue.Contains(startCharacter) && merchantDefinedValue.Contains(endCharacter) && sanityCheck < 100);
                                }

                                MerchantDefinedInformation merchantDefinedInformation = new MerchantDefinedInformation
                                {
                                    key = merchantDefinedValueKey,
                                    value = merchantDefinedValue
                                };

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
                }
            }
            catch(Exception ex)
            {
                _context.Vtex.Logger.Error("GetMerchantDefinedInformation", null, null, ex);
            }

            return merchantDefinedInformationList;
        }
    }
}