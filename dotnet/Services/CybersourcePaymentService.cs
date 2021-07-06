using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
        private readonly string _applicationName;

        public CybersourcePaymentService(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ICybersourceApi cybersourceApi, ICybersourceRepository cybersourceRepository)
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

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest)
        {
            //_context.Vtex.Logger.Debug("CreatePayment", null, JsonConvert.SerializeObject(createPaymentRequest));
            CreatePaymentResponse createPaymentResponse = null;
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = createPaymentRequest.PaymentId,
                    //transactionId = createPaymentRequest.TransactionId
                },
                paymentInformation = new PaymentInformation
                {
                    card = new Card
                    {
                        number = createPaymentRequest.Card.Number,
                        securityCode = createPaymentRequest.Card.Csc,
                        expirationMonth = createPaymentRequest.Card.Expiration.Month,
                        expirationYear = createPaymentRequest.Card.Expiration.Year
                    }
                },
                orderInformation = new OrderInformation
                {
                    amountDetails = new AmountDetails
                    {
                        totalAmount = createPaymentRequest.Value.ToString(),
                        currency = createPaymentRequest.Currency
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
                        country = createPaymentRequest.MiniCart.BillingAddress.Country.Substring(0,2),
                        email = createPaymentRequest.MiniCart.Buyer.Email,
                        phoneNumber = createPaymentRequest.MiniCart.Buyer.Phone
                    },
                    lineItems = new System.Collections.Generic.List<LineItem>()
                }
            };

            foreach(VtexItem vtexItem in createPaymentRequest.MiniCart.Items)
            {
                LineItem lineItem = new LineItem
                {
                    productSku = vtexItem.Id,
                    productName = vtexItem.Name,
                    unitPrice = vtexItem.Price.ToString(),
                    quantity = vtexItem.Quantity.ToString(),
                    discountAmount = vtexItem.Discount.ToString()
                };

                payment.orderInformation.lineItems.Add(lineItem);
            }

            PaymentsResponse paymentsResponse = await _cybersourceApi.ProcessPayment(payment);
            if(paymentsResponse != null)
            {
                createPaymentResponse = new CreatePaymentResponse();
                createPaymentResponse.AuthorizationId = paymentsResponse.Id;
                createPaymentResponse.Tid = paymentsResponse.Id;
                createPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;
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
                switch(paymentsResponse.Status)
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
                if(paymentsResponse.ProcessorInformation != null)
                {
                    createPaymentResponse.Nsu = paymentsResponse.ProcessorInformation.TransactionId;
                }
            
                createPaymentResponse.PaymentId = createPaymentRequest.PaymentId;

                decimal authAmount = 0m;
                if(paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
                {
                    decimal.TryParse(paymentsResponse.OrderInformation.amountDetails.authorizedAmount, out authAmount);
                }

                PaymentData paymentData = new PaymentData
                {
                    AuthorizationId = createPaymentResponse.AuthorizationId,
                    TransactionId = createPaymentResponse.Tid,
                    PaymentId = createPaymentResponse.PaymentId,
                    Value = authAmount,
                    RequestId = null,
                    CaptureId = null
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
                    code = cancelPaymentRequest.PaymentId
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
            if(paymentsResponse != null)
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
                    code = capturePaymentRequest.PaymentId
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
            if(paymentsResponse != null)
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
                if(paymentsResponse.OrderInformation != null && paymentsResponse.OrderInformation.amountDetails != null)
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
                    code = refundPaymentRequest.PaymentId
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
            if(paymentsResponse != null)
            {
                refundPaymentResponse = new RefundPaymentResponse();
                refundPaymentResponse.PaymentId = refundPaymentRequest.PaymentId;
                refundPaymentResponse.RequestId = refundPaymentRequest.RequestId;
                refundPaymentResponse.Message = paymentsResponse.ErrorInformation != null ? paymentsResponse.ErrorInformation.Message : paymentsResponse.Message;
                refundPaymentResponse.RefundId = paymentsResponse.Id;
                refundPaymentResponse.Code = paymentsResponse.ProcessorInformation != null ? paymentsResponse.ProcessorInformation.ResponseCode : paymentsResponse.Status;

                if(paymentsResponse.RefundAmountDetails != null && paymentsResponse.RefundAmountDetails.RefundAmount != null)
                {
                    refundPaymentResponse.Value = decimal.Parse(paymentsResponse.RefundAmountDetails.RefundAmount);
                }
            }

            return refundPaymentResponse;
        }

        public async Task<SendAntifraudDataResponse> SendAntifraudData(SendAntifraudDataRequest sendAntifraudDataRequest)
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;
            
            Payments payment = new Payments
            {
                clientReferenceInformation = new ClientReferenceInformation
                {
                    code = sendAntifraudDataRequest.Id,
                    comments = sendAntifraudDataRequest.Reference
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
                        country = sendAntifraudDataRequest.MiniCart.Buyer.Address.Country.Substring(0,2),
                        email = sendAntifraudDataRequest.MiniCart.Buyer.Email,
                        phoneNumber = sendAntifraudDataRequest.MiniCart.Buyer.Phone
                    },
                    lineItems = new System.Collections.Generic.List<LineItem>()
                }
            };

            foreach(AntifraudItem vtexItem in sendAntifraudDataRequest.MiniCart.Items)
            {
                LineItem lineItem = new LineItem
                {
                    productSku = vtexItem.Id,
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

            switch(paymentsResponse.Status)
            {
                case "ACCEPTED":
                    sendAntifraudDataResponse.Status = CybersourceConstants.VtexAuthStatus.Approved;
                    break;
                case "PENDING_REVIEW":
                case "PENDING_AUTHENTICATION":
                case "INVALID_REQUEST":
                case "CHALLENGE":
                    sendAntifraudDataResponse.Status = CybersourceConstants.VtexAuthStatus.Undefined;
                    break;
                case "REJECTED":
                case "DECLINED":
                case "AUTHENTICATION_FAILED":
                    sendAntifraudDataResponse.Status = CybersourceConstants.VtexAuthStatus.Denied;
                    break;
            };

            string riskInfo = JsonConvert.SerializeObject(paymentsResponse.RiskInformation);
            Dictionary<string, object> riskDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(riskInfo);
            Func<Dictionary<string, object>, IEnumerable<KeyValuePair<string, object>>> flatten = null;
            flatten = dict => dict.SelectMany(kv => 
                        kv.Value is Dictionary<string,object> 
                            ? flatten((Dictionary<string,object>)kv.Value)
                            : new List<KeyValuePair<string,object>>(){ kv}
                       );

            sendAntifraudDataResponse.Responses = flatten(riskDictionary).ToDictionary(x => x.Key, x => x.Value.ToString());

            await _cybersourceRepository.SaveAntifraudData(sendAntifraudDataRequest.Id, sendAntifraudDataResponse);

            sendAntifraudDataResponse.Status = CybersourceConstants.VtexAntifraudStatus.Received;

            return sendAntifraudDataResponse;
        }

        public async Task<SendAntifraudDataResponse> GetAntifraudStatus(string id)
        {
            return await _cybersourceRepository.GetAntifraudData(id);
        }
    }
}