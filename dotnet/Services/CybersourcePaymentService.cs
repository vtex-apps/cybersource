using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                Console.WriteLine("Returning CreatePaymentResponse from storage.");
                _context.Vtex.Logger.Debug("CreatePayment", null, "Returning CreatePaymentResponse from storage.");
                return paymentData.CreatePaymentResponse;
            }

            _context.Vtex.Logger.Debug("CreatePayment", null, JsonConvert.SerializeObject(createPaymentRequest));
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = createPaymentRequest.PaymentId,
                    //transactionId = createPaymentRequest.TransactionId,
                    applicationName = _context.Vtex.App.Name,
                    applicationVersion = _context.Vtex.App.Version,
                    applicationUser = _context.Vtex.App.Vendor
                },
                paymentInformation = new PaymentInformation
                {
                    card = new Card
                    {
                        number = createPaymentRequest.Card.NumberToken ?? createPaymentRequest.Card.Number,
                        securityCode = createPaymentRequest.Card.CscToken ?? createPaymentRequest.Card.Csc,
                        expirationMonth = createPaymentRequest.Card.Expiration.Month,
                        expirationYear = createPaymentRequest.Card.Expiration.Year,
                        type = GetCardType(createPaymentRequest.PaymentMethod)
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
                //installmentInformation = new InstallmentInformation
                //{
                //    amount = createPaymentRequest.InstallmentsValue.ToString(),
                //    totalCount = createPaymentRequest.Installments.ToString()
                //},
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

            string numberOfInstallments = createPaymentRequest.Installments.ToString("00");
            string plan = string.Empty;
            decimal installmentsInterestRate = createPaymentRequest.InstallmentsInterestRate;
            Console.WriteLine($"    ------------------ Processor: {merchantSettings.Processor} ------------------    ");
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
                            reconciliationId = ""
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
                    payment.processingInformation = new ProcessingInformation
                    {
                        commerceIndicator = CybersourceConstants.INSTALLMENT
                    };

                    payment.installmentInformation = new InstallmentInformation
                    {
                        totalCount = numberOfInstallments
                    };

                    break;
                case CybersourceConstants.Processors.Izipay:
                    plan = "0";  // 0: no deferred payment, 1: 30 días, 2: 60 días, 3: 90 días
                    payment.issuerInformation = new IssuerInformation
                    {
                        //POS 1 - 6:014001
                        //POS 7 - 8: # of installments
                        //POS 9 - 16:00000000
                        //POS 17: plan(0: no deferred payment, 1: 30 días, 2: 60 días, 3: 90 días)
                        discretionaryData = $"14001{numberOfInstallments}00000000{plan}"
                    };

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
                        reconciliationId = ""
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
                        reconciliationId = ""
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

            foreach (VtexItem vtexItem in createPaymentRequest.MiniCart.Items)
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

            PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessPayment(payment, createPaymentRequest.SecureProxyUrl, createPaymentRequest.SecureProxyTokensUrl);
            if (paymentsResponse != null)
            {
                createPaymentResponse = new CreatePaymentResponse();
                createPaymentResponse.AuthorizationId = paymentsResponse.Id;
                createPaymentResponse.Tid = paymentsResponse.Id;
                createPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message != null ? paymentsResponse.Message : paymentsResponse.Status;
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
                        paymentStatus = CybersourceConstants.VtexAuthStatus.Approved;
                        break;
                    case "AUTHORIZED_PENDING_REVIEW":
                    case "PENDING_AUTHENTICATION":
                    case "PENDING_REVIEW":
                    case "INVALID_REQUEST":
                        paymentStatus = CybersourceConstants.VtexAuthStatus.Undefined;
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
                    CallbackUrl = createPaymentRequest.CallbackUrl
                };

                await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
            }

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
                    code = cancelPaymentRequest.PaymentId,
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
                    code = capturePaymentRequest.PaymentId,
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
                    code = refundPaymentRequest.PaymentId,
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

            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = sendAntifraudDataRequest.Id,
                    comments = sendAntifraudDataRequest.Reference,
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
                    sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Approved;
                    break;
                case "PENDING_REVIEW":
                case "PENDING_AUTHENTICATION":
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
            };

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

        public async Task<string> ProcessConversions()
        {
            string results = string.Empty;
            StringBuilder sb = new StringBuilder();
            DateTime dtStartTime = DateTime.Now.AddDays(-1);
            DateTime dtEndTime = DateTime.Now;
            ConversionReportResponse conversionReport = await this.ConversionDetailReport(dtStartTime, dtEndTime);
            if(conversionReport != null)
            {
                foreach(ConversionDetail conversionDetail in conversionReport.ConversionDetails)
                {
                    //sb.AppendLine($"{conversionDetail.MerchantReferenceNumber} {conversionDetail.OriginalDecision} - {conversionDetail.NewDecision} ");
                    PaymentData paymentData = await _cybersourceRepository.GetPaymentData(conversionDetail.MerchantReferenceNumber);
                    if(paymentData != null && paymentData.CreatePaymentResponse != null)
                    {
                        if(paymentData.CreatePaymentResponse.Status.Equals(CybersourceConstants.VtexAuthStatus.Undefined))
                        {
                            bool updateStatus = true;
                            switch(conversionDetail.NewDecision)
                            {
                                case CybersourceConstants.CybersourceDecision.Accept:
                                    paymentData.CreatePaymentResponse.Status = CybersourceConstants.VtexAuthStatus.Approved;
                                    break;
                                case CybersourceConstants.CybersourceDecision.Reject:
                                    paymentData.CreatePaymentResponse.Status = CybersourceConstants.VtexAuthStatus.Denied;
                                    break;
                                case CybersourceConstants.CybersourceDecision.Review:
                                    updateStatus = false;
                                    break;
                            }

                            if(updateStatus)
                            {
                                if (paymentData.CallbackUrl != null)
                                {
                                    SendResponse sendResponse = await _vtexApiService.PostCallbackResponse(paymentData.CallbackUrl, paymentData.CreatePaymentResponse);
                                    if (sendResponse != null)
                                    {
                                        sb.AppendLine($"{conversionDetail.MerchantReferenceNumber} {conversionDetail.OriginalDecision} - {conversionDetail.NewDecision} updated? {sendResponse.Success}");
                                        Console.WriteLine($"{conversionDetail.MerchantReferenceNumber} {conversionDetail.OriginalDecision} -> {conversionDetail.NewDecision} updated? {sendResponse.Success}");
                                        await _cybersourceRepository.SavePaymentData(paymentData.PaymentId, paymentData);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            results = sb.ToString();

            return results;
        }

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
    }
}