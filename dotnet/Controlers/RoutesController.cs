namespace service.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Cybersource.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Cybersource.Models;
    using Newtonsoft.Json;
    using Cybersource.Data;
    using Vtex.Api.Context;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Xml.Serialization;
    using System.IO;

    public class RoutesController : Controller
    {
        private readonly ICybersourcePaymentService _cybersourcePaymentService;
        private readonly ICybersourceRepository _cybersourceRepository;
        private readonly IVtexApiService _vtexApiService;
        private readonly IIOServiceContext _context;

        public RoutesController(ICybersourcePaymentService cybersourcePaymentService, ICybersourceRepository cybersourceRepository, IVtexApiService vtexApiService, IIOServiceContext context)
        {
            this._cybersourcePaymentService = cybersourcePaymentService ?? throw new ArgumentNullException(nameof(cybersourcePaymentService));
            this._cybersourceRepository = cybersourceRepository ?? throw new ArgumentNullException(nameof(cybersourceRepository));
            this._vtexApiService = vtexApiService ?? throw new ArgumentNullException(nameof(vtexApiService));
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments
        /// Creates a new payment and/or initiates the payment flow.
        /// </summary>
        /// <param name="createPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CreatePayment()
        {
            CreatePaymentResponse createPaymentResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                try
                {
                    CreatePaymentRequest createPaymentRequest = JsonConvert.DeserializeObject<CreatePaymentRequest>(bodyAsText);
                    MerchantSetting merchantSettingPayerAuth = null;
                    MerchantSetting merchantSettingCaptureDelay = null;
                    MerchantSetting merchantSettingCaptureDelayInterval = null;
                    long captureDelay = 0L;
                    string delayInterval = string.Empty;
                    bool doPayerAuth = false;
                    try
                    {
                        if (createPaymentRequest.MerchantSettings != null)
                        {
                            merchantSettingPayerAuth = createPaymentRequest.MerchantSettings.FirstOrDefault(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.UsePayerAuth));
                            if (merchantSettingPayerAuth != null && merchantSettingPayerAuth.Value != null && merchantSettingPayerAuth.Value.Equals(CybersourceConstants.ManifestCustomField.Active, StringComparison.OrdinalIgnoreCase))
                            {
                                doPayerAuth = true;
                            }

                            merchantSettingCaptureDelay = createPaymentRequest.MerchantSettings.FirstOrDefault(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.CaptureDelay));
                            if (merchantSettingCaptureDelay != null && merchantSettingCaptureDelay.Value != null)
                            {
                                long.TryParse(merchantSettingCaptureDelay.Value, out captureDelay);
                            }

                            merchantSettingCaptureDelayInterval = createPaymentRequest.MerchantSettings.FirstOrDefault(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.CaptureDelayInterval));
                            if (merchantSettingCaptureDelayInterval != null && merchantSettingCaptureDelayInterval.Value != null)
                            {
                                delayInterval = merchantSettingCaptureDelayInterval.Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Vtex.Logger.Error("CreatePayment", "UsePayerAuth",
                        "Error: ", ex,
                        new[]
                        {
                        ("body", bodyAsText)
                        });
                    }

                    if (doPayerAuth)
                    {
                        createPaymentResponse = await this._cybersourcePaymentService.SetupPayerAuth(createPaymentRequest);
                    }
                    else
                    {
                        //(createPaymentResponse, paymentsResponse) = await this._cybersourcePaymentService.CreatePayment(createPaymentRequest);
                        var createPaymentTask = this._cybersourcePaymentService.CreatePayment(createPaymentRequest);
                        var winner = await Task.WhenAny(createPaymentTask, DelayedDummyResultTask<(CreatePaymentResponse, PaymentsResponse)>(TimeSpan.FromSeconds(20)));
                        if (winner == createPaymentTask)
                        {
                            //_context.Vtex.Logger.Debug("CreatePayment", "Timeout", $"Processed {createPaymentRequest.PaymentId} in time!");
                            createPaymentResponse = winner.Result.Item1;
                        }
                        else
                        {
                            createPaymentResponse = createPaymentTask.Result.Item1;
                            PaymentsResponse paymentsResponse = createPaymentTask.Result.Item2;
                            PaymentData paymentData = new PaymentData();
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

                            if (!paymentsResponse.Status.Equals("ERROR"))
                            {
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

                                paymentData.TimedOut = true;
                            }

                            await _cybersourceRepository.SavePaymentData(createPaymentRequest.PaymentId, paymentData);
                            _context.Vtex.Logger.Warn("CreatePayment", "Timeout", $"PaymentId {createPaymentRequest.PaymentId} Timed out.");

                            return StatusCode(504, createPaymentResponse);
                        }
                    }

                    if (!string.IsNullOrEmpty(delayInterval) && createPaymentResponse != null)
                    {
                        int multiple = 1;
                        switch (delayInterval)
                        {
                            case CybersourceConstants.CaptureIntervalSetting.Minutes:
                                multiple = 60;
                                break;
                            case CybersourceConstants.CaptureIntervalSetting.Hours:
                                multiple = 60 * 60;
                                break;
                            case CybersourceConstants.CaptureIntervalSetting.Days:
                                multiple = 60 * 60 * 24;
                                break;
                        }

                        createPaymentResponse.DelayToAutoSettle = captureDelay * multiple;
                    }
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("CreatePayment", null,
                    "Payment Error: ", ex,
                    new[]
                    {
                        ("body", bodyAsText)
                    });
                }
            }

            Response.Headers.Add("Cache-Control", "private");
            return Json(createPaymentResponse);
        }

        /// <summary>
        /// Called from cybersource-payer-auth app
        /// Calls Check Payer Auth Enrollment API
        /// /risk/v1/authentications
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public async Task<IActionResult> PayerAuth(string paymentId)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string paymentStatus;
            bool doCancel;
            CreatePaymentResponse createPaymentResponse = new CreatePaymentResponse();
            PaymentsResponse paymentsResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(paymentId);
            if (paymentData != null && paymentData.CreatePaymentRequest != null)
            {
                if (!string.IsNullOrEmpty(paymentData.PayerAuthReferenceId))
                {
                    paymentsResponse = await _cybersourcePaymentService.CheckPayerAuthEnrollment(paymentData);
                    (createPaymentResponse, paymentsResponse, paymentStatus, doCancel) = await _cybersourcePaymentService.GetPaymentStatus(createPaymentResponse, paymentData.CreatePaymentRequest, paymentsResponse, true);
                    if (paymentStatus.Equals(CybersourceConstants.VtexAuthStatus.Approved))
                    {
                        // If Enrollment Check is Approved, create payment
                        (createPaymentResponse, paymentsResponse) = await this._cybersourcePaymentService.CreatePayment(paymentData.CreatePaymentRequest, paymentsResponse.ConsumerAuthenticationInformation.AuthenticationTransactionId, paymentsResponse.ConsumerAuthenticationInformation);
                        paymentData.CreatePaymentResponse = createPaymentResponse;
                        await _cybersourceRepository.SavePaymentData(paymentId, paymentData);
                        //await _vtexApiService.PostCallbackResponse(paymentData.CreatePaymentRequest.CallbackUrl, paymentData.CreatePaymentResponse);
                    }
                    else
                    {
                        if (paymentData.CreatePaymentResponse == null)
                        {
                            paymentData.CreatePaymentResponse = new CreatePaymentResponse
                            {
                                PaymentId = paymentId,
                                Status = paymentStatus
                            };
                        }
                        else
                        {
                            paymentData.CreatePaymentResponse.Status = paymentStatus;
                        }

                        await _cybersourceRepository.SavePaymentData(paymentId, paymentData);
                        if (paymentStatus.Equals(CybersourceConstants.VtexAuthStatus.Denied))
                        {
                            // If Enrollment Check is Denied, update checkout
                            _ = _vtexApiService.PostCallbackResponse(paymentData.CreatePaymentRequest.CallbackUrl, paymentData.CreatePaymentResponse);
                        }
                    }
                }
                else
                {
                    paymentsResponse = new PaymentsResponse
                    {
                        Status = CybersourceConstants.VtexAuthStatus.Denied,
                        Message = "Missing PayerAuthReferenceId"
                    };

                    _context.Vtex.Logger.Error("PayerAuth", null, "Missing PayerAuthReferenceId");
                }
            }

            return Json(paymentsResponse);
        }

        /// <summary>
        /// Receives Post from challenge window
        /// </summary>
        /// <returns></returns>
        public async Task PayerAuthResponse()
        {
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                try
                {
                    var queryParams = HttpUtility.ParseQueryString(bodyAsText);
                    string authenticationTransactionId = queryParams["TransactionId"];
                    string paymentId = queryParams["MD"];
                    await this.ProcessPayerAuthentication(paymentId, authenticationTransactionId);
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("PayerAuthResponse", null,
                    "Payment Error: ", ex,
                    new[]
                    {
                        ("body", bodyAsText)
                    });
                }
            }
        }

        public async Task ValidateAuthenticationResults(string paymentId, string authenticationTransactionId)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            await this.ProcessPayerAuthentication(paymentId, authenticationTransactionId);
        }

        /// <summary>
        /// Called from cybersource-payer-auth app to check stored status
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public async Task<IActionResult> CheckAuthStatus(string paymentId)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string status = CybersourceConstants.VtexAuthStatus.Undefined;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(paymentId);
            if (paymentData != null && paymentData.CreatePaymentResponse != null)
            {
                status = paymentData.CreatePaymentResponse.Status;
                if (!status.Equals(CybersourceConstants.VtexAuthStatus.Undefined))
                {
                    SendResponse sendResponse = await _vtexApiService.PostCallbackResponse(paymentData.CreatePaymentRequest.CallbackUrl, paymentData.CreatePaymentResponse);
                    if (sendResponse.Success)
                    {
                        TransactionDetails transactionDetails = await _vtexApiService.GetTransactionDetails(paymentData.TransactionId);
                        if (transactionDetails != null)
                        {
                            status = transactionDetails.Status.ToLower();
                        }
                    }
                }
            }

            return Json(status);
        }

        /// <summary>
        /// Calls Validate Authentication Results API
        /// /risk/v1/authentication-results
        /// </summary>
        /// <param name="paymentId">VTEX Payment Id</param>
        /// <param name="authenticationTransactionId"></param>
        /// <returns></returns>
        public async Task<CreatePaymentResponse> ProcessPayerAuthentication(string paymentId, string authenticationTransactionId)
        {
            CreatePaymentResponse createPaymentResponse = new CreatePaymentResponse
            {
                PaymentId = paymentId
            };

            PaymentsResponse paymentsResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(paymentId);
            if (paymentData != null && paymentData.CreatePaymentRequest != null)
            {
                if (!string.IsNullOrEmpty(authenticationTransactionId))
                {
                    string paymentStatus;
                    bool doCancel;
                    paymentData.AuthenticationTransactionId = authenticationTransactionId;
                    await _cybersourceRepository.SavePaymentData(paymentId, paymentData);
                    paymentsResponse = await _cybersourcePaymentService.ValidateAuthenticationResults(paymentData.CreatePaymentRequest, authenticationTransactionId);
                    (createPaymentResponse, paymentsResponse, paymentStatus, doCancel) = await _cybersourcePaymentService.GetPaymentStatus(createPaymentResponse, paymentData.CreatePaymentRequest, paymentsResponse, true);
                    paymentData.CreatePaymentResponse = createPaymentResponse;
                    // If Validation is Approved do authorization
                    if (paymentStatus.Equals(CybersourceConstants.VtexAuthStatus.Approved))
                    {
                        (createPaymentResponse, paymentsResponse) = await this._cybersourcePaymentService.CreatePayment(paymentData.CreatePaymentRequest, authenticationTransactionId, paymentsResponse.ConsumerAuthenticationInformation);
                    }
                    else
                    {
                        await _cybersourceRepository.SavePaymentData(paymentId, paymentData);
                    }

                    _context.Vtex.Logger.Info("ValidateAuthenticationResults", null, $"{paymentData.CreatePaymentRequest.OrderId} = {paymentData.CreatePaymentResponse.Status}", new[]
                    {
                        ("paymentId", paymentId),
                        ("authenticationTransactionId", authenticationTransactionId),
                        ("createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse)),
                        ("paymentsResponse", JsonConvert.SerializeObject(paymentsResponse))
                    });
                }
                else
                {
                    createPaymentResponse = new CreatePaymentResponse
                    {
                        Message = "Missing AuthenticationTransactionId"
                    };

                    _context.Vtex.Logger.Debug("ValidateAuthenticationResults", null, "Missing AuthenticationTransactionId");
                }
            }

            return createPaymentResponse;
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/cancellations
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="cancelPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CancelPayment(string paymentId)
        {
            CancelPaymentResponse cancelResponse = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    _context.Vtex.Logger.Debug("CancelPayment", "bodyAsText", bodyAsText);
                    CancelPaymentRequest cancelPaymentRequest = JsonConvert.DeserializeObject<CancelPaymentRequest>(bodyAsText);
                    cancelResponse = await this._cybersourcePaymentService.CancelPayment(cancelPaymentRequest);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CancelPayment", null,
                "Error: ", ex,
                new[]
                {
                    ("paymentId", paymentId)
                });
            }

            return Json(cancelResponse);
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/settlements
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="capturePaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CapturePayment(string paymentId)
        {
            CapturePaymentResponse captureResponse = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    CapturePaymentRequest capturePaymentRequest = JsonConvert.DeserializeObject<CapturePaymentRequest>(bodyAsText);
                    captureResponse = await this._cybersourcePaymentService.CapturePayment(capturePaymentRequest);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CapturePayment", null,
                "Error: ", ex,
                new[]
                {
                    ("paymentId", paymentId)
                });
            }

            return Json(captureResponse);
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/refunds
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="refundPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> RefundPayment(string paymentId)
        {
            RefundPaymentResponse refundResponse = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    RefundPaymentRequest refundPaymentRequest = JsonConvert.DeserializeObject<RefundPaymentRequest>(bodyAsText);
                    refundResponse = await this._cybersourcePaymentService.RefundPayment(refundPaymentRequest);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("RefundPayment", null,
                "Error: ", ex,
                new[]
                {
                    ("paymentId", paymentId)
                });
            }

            return Json(refundResponse);
        }

        public JsonResult PaymentMethods()
        {
            PaymentMethodsList methods = new PaymentMethodsList();
            methods.PaymentMethods = new List<string>();
            methods.PaymentMethods.Add("Visa");
            methods.PaymentMethods.Add("Mastercard");
            methods.PaymentMethods.Add("American Express");
            methods.PaymentMethods.Add("Discover");
            methods.PaymentMethods.Add("JCB");
            methods.PaymentMethods.Add("Diners");
            methods.PaymentMethods.Add("Hipercard");
            methods.PaymentMethods.Add("Elo");

            return Json(methods);
        }

        public JsonResult Manifest()
        {
            Manifest manifest = new Manifest
            {
                PaymentMethods = new List<PaymentMethod>
                {
                    new PaymentMethod
                    {
                        Name = "Visa",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "American Express",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "Diners",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "Mastercard",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "Discover",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "Hipercard",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "Elo",
                        AllowsSplit = "onCapture"
                    },
                    new PaymentMethod
                    {
                        Name = "JCB",
                        AllowsSplit = "onCapture"
                    }
                },
                CustomFields = new List<object>
                {
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.CompanyName,
                        Type = "text"
                    },
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.CompanyTaxId,
                        Type = "text"
                    },
                    new CustomFieldOptions
                    {
                        Name = CybersourceConstants.ManifestCustomField.UsePayerAuth,
                        Type = "select",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Disabled,
                                Value = CybersourceConstants.PayerAuthenticationSetting.Disabled
                            },
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Active,
                                Value = CybersourceConstants.PayerAuthenticationSetting.Active
                            }
                        }
                    },
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.CaptureDelay,
                        Type = "text"
                    },
                    new CustomFieldOptions
                    {
                        Name = CybersourceConstants.ManifestCustomField.CaptureDelayInterval,
                        Type = "select",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Text = CybersourceConstants.CaptureIntervalSetting.Minutes,
                                Value = CybersourceConstants.CaptureIntervalSetting.Minutes
                            },
                            new Option
                            {
                                Text = CybersourceConstants.CaptureIntervalSetting.Hours,
                                Value = CybersourceConstants.CaptureIntervalSetting.Hours
                            },
                            new Option
                            {
                                Text = CybersourceConstants.CaptureIntervalSetting.Days,
                                Value = CybersourceConstants.CaptureIntervalSetting.Days
                            }
                        }
                    },
                    new CustomFieldOptions
                    {
                        Name = CybersourceConstants.ManifestCustomField.CaptureSetting,
                        Type = "select",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.DelayedCapture,
                                Value = CybersourceConstants.CaptureSetting.DelayedCapture
                            },
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.ImmediateCapture,
                                Value = CybersourceConstants.CaptureSetting.ImmediateCapture
                            }
                        }
                    },
                    new CustomFieldOptions
                    {
                        Name = CybersourceConstants.ManifestCustomField.DecisionManagerInUse,
                        Type = "select",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Disabled,
                                Value = CybersourceConstants.PayerAuthenticationSetting.Disabled
                            },
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Active,
                                Value = CybersourceConstants.PayerAuthenticationSetting.Active
                            }
                        }
                    },
                    new CustomFieldOptions
                    {
                        Name = CybersourceConstants.ManifestCustomField.AuthorizedRiskDeclined,
                        Type = "select",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Accept,
                                Value = CybersourceConstants.VtexAuthStatus.Approved
                            },
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Decline,
                                Value = CybersourceConstants.VtexAuthStatus.Denied
                            },
                            new Option
                            {
                                Text = CybersourceConstants.ManifestCustomField.Pending,
                                Value = CybersourceConstants.VtexAuthStatus.Undefined
                            }
                        }
                    },
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.MerchantId,
                        Type = "text"
                    },
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.MerchantKey,
                        Type = "text"
                    },
                    new CustomField
                    {
                        Name = CybersourceConstants.ManifestCustomField.SharedSecretKey,
                        Type = "text"
                    }
                }
            };

            Response.Headers.Add("Cache-Control", "private");
            return Json(manifest);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/transactions
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> SendAntifraudData()
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    SendAntifraudDataRequest sendAntifraudDataRequest = JsonConvert.DeserializeObject<SendAntifraudDataRequest>(bodyAsText);
                    sendAntifraudDataResponse = await this._cybersourcePaymentService.SendAntifraudData(sendAntifraudDataRequest);
                    sw.Stop();
                    _context.Vtex.Logger.Debug("SendAntifraudData", null, $"Elapsed Time = '{sw.Elapsed.TotalMilliseconds}' ", new[]
                    {
                        ("sendAntifraudDataRequest", bodyAsText),
                        ("sendAntifraudDataResponse", JsonConvert.SerializeObject(sendAntifraudDataResponse))
                    });
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendAntifraudData", null, "Error: ", ex);
            }

            return Json(sendAntifraudDataResponse);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/pre-analysis
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> SendAntifraudPreAnalysisData()
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = new SendAntifraudDataResponse { Status = CybersourceConstants.VtexAntifraudStatus.Received };

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    SendAntifraudDataRequest sendAntifraudDataRequest = JsonConvert.DeserializeObject<SendAntifraudDataRequest>(bodyAsText);
                    sendAntifraudDataResponse = await this._cybersourcePaymentService.SendAntifraudData(sendAntifraudDataRequest);
                    sw.Stop();
                    _context.Vtex.Logger.Debug("SendAntifraudPreAnalysisData", null, $"Elapsed Time = '{sw.Elapsed.TotalMilliseconds}' ", new[]
                    {
                        ("sendAntifraudDataRequest", bodyAsText),
                        ("sendAntifraudDataResponse", JsonConvert.SerializeObject(sendAntifraudDataResponse))
                    });
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendAntifraudPreAnalysisData", null, "Error: ", ex);
            }

            return Json(sendAntifraudDataResponse);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/transactions/transactions.id
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetAntifraudStatus(string transactionId)
        {
            SendAntifraudDataResponse getAntifraudStatusResponse = null;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                getAntifraudStatusResponse = await this._cybersourcePaymentService.GetAntifraudStatus(transactionId);
                if (getAntifraudStatusResponse == null)
                {
                    getAntifraudStatusResponse = new SendAntifraudDataResponse
                    {
                        Id = transactionId,
                        Status = CybersourceConstants.VtexAntifraudStatus.Undefined
                    };
                }

                sw.Stop();
                _context.Vtex.Logger.Debug("GetAntifraudStatus", transactionId, $"Returned {getAntifraudStatusResponse.Status} in {sw.Elapsed.TotalMilliseconds} ms ", new[]
                {
                    ("getAntifraudStatusResponse",
                    JsonConvert.SerializeObject(getAntifraudStatusResponse))
                });
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetAntifraudStatus", null,
                "Error: ", ex,
                new[]
                {
                    ("transactionId", transactionId)
                });
            }

            return Json(getAntifraudStatusResponse);
        }

        public async Task<IActionResult> TaxHandler()
        {
            string orderFormId = string.Empty;
            string totalItems = string.Empty;
            bool fromCache = false;
            VtexTaxRequest taxRequest = null;
            VtexTaxRequest taxRequestOriginal = null;
            VtexTaxResponse vtexTaxResponse = new VtexTaxResponse
            {
                ItemTaxResponse = new List<ItemTaxResponse>()
            };

            try
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                Response.Headers.Add("Cache-Control", "private");
                Response.Headers.Add(CybersourceConstants.CONTENT_TYPE, CybersourceConstants.MINICART);
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    if (!string.IsNullOrEmpty(bodyAsText))
                    {
                        taxRequest = JsonConvert.DeserializeObject<VtexTaxRequest>(bodyAsText);
                        taxRequestOriginal = JsonConvert.DeserializeObject<VtexTaxRequest>(bodyAsText);
                        if (taxRequest != null)
                        {
                            orderFormId = taxRequest.OrderFormId;
                            totalItems = taxRequest.Items.Sum(i => i.Quantity).ToString();
                            decimal total = taxRequest.Totals.Sum(t => t.Value);
                            int cacheKey = $"{_context.Vtex.App.Version}{taxRequest.ShippingDestination.PostalCode}{totalItems}{total}".GetHashCode();
                            if (_cybersourceRepository.TryGetCache(cacheKey, out vtexTaxResponse))
                            {
                                fromCache = true;
                                _context.Vtex.Logger.Debug("CybersourceOrderTaxHandler", null, $"Taxes for '{cacheKey}' fetched from cache. {JsonConvert.SerializeObject(vtexTaxResponse)}");
                            }
                            else
                            {
                                vtexTaxResponse = await _vtexApiService.GetTaxes(taxRequest, taxRequestOriginal);
                                if (vtexTaxResponse != null)
                                {
                                    await _cybersourceRepository.SetCache(cacheKey, vtexTaxResponse);
                                }
                                else
                                {
                                    vtexTaxResponse = await _vtexApiService.GetFallbackTaxes(taxRequestOriginal);
                                    _context.Vtex.Logger.Error("TaxHandler", "Fallback", "Using Fallback Rates", null, new[]
                                    {
                                        ("VtexTaxRequest", JsonConvert.SerializeObject(taxRequestOriginal)),
                                        ("VtexTaxResponse", JsonConvert.SerializeObject(vtexTaxResponse))
                                    });
                                }
                            }
                        }
                    }
                }

                timer.Stop();
                _context.Vtex.Logger.Debug("TaxHandler", "Response", $"Elapsed Time = '{timer.Elapsed.TotalMilliseconds}' '{orderFormId}' {totalItems} items.  From cache? {fromCache}", new[]
                {
                    ("VtexTaxRequest", JsonConvert.SerializeObject(taxRequestOriginal)),
                    ("VtexTaxResponse", JsonConvert.SerializeObject(vtexTaxResponse))
                });
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("TaxHandler", null, "Error: ", ex);
            }

            return Json(vtexTaxResponse);
        }

        public async Task<IActionResult> ToggleTax(bool useCyberTax)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string result = string.Empty;

            try
            {
                if (useCyberTax)
                {
                    result = await _vtexApiService.InitConfiguration();
                }
                else
                {
                    result = await _vtexApiService.RemoveConfiguration();
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ToggleTax", null,
                "Error: ", ex,
                new[]
                {
                    ( "useCyberTax", useCyberTax.ToString() )
                });
            }

            return Json(result);
        }

        public async Task<IActionResult> CybersourceResponseToVtexResponse()
        {
            VtexTaxResponse vtexTaxResponse = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    PaymentsResponse taxResponse = JsonConvert.DeserializeObject<PaymentsResponse>(bodyAsText);
                    vtexTaxResponse = await this._vtexApiService.CybersourceResponseToVtexResponse(taxResponse, null);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CybersourceResponseToVtexResponse", null, "Error: ", ex);
            }

            return Json(vtexTaxResponse);
        }

        public async Task<IActionResult> HealthCheck()
        {
            return Json("--result goes here");
        }

        public async Task<IActionResult> ConversionDetailReport()
        {
            Response.Headers.Add("Cache-Control", "no-cache");

            return Json(await _cybersourcePaymentService.ConversionDetailReport(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(0)));
        }

        public async Task<IActionResult> RetrieveAvailableReports()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(await _cybersourcePaymentService.RetrieveAvailableReports(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(0)));
        }

        public async Task<IActionResult> GetPurchaseAndRefundDetails()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(await _cybersourcePaymentService.GetPurchaseAndRefundDetails(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(0)));
        }

        public async Task<IActionResult> ProcessConversions()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(await _vtexApiService.ProcessConversions());
        }

        public async Task<IActionResult> DecisionManagerNotify()
        {
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string result = string.Empty;
                try
                {
                    string bodyAsTextRaw = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    try
                    {
                        string bodyAsText = HttpUtility.UrlDecode(bodyAsTextRaw);
                        bodyAsText = bodyAsText.Substring(bodyAsText.IndexOf("=") + 1);
                        try
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(CaseManagementOrderStatus));
                            CaseManagementOrderStatus caseManagementOrderStatus;

                            using (TextReader reader = new StringReader(bodyAsText))
                            {
                                caseManagementOrderStatus = (CaseManagementOrderStatus)serializer.Deserialize(reader);
                            }

                            result = await _vtexApiService.UpdateOrderStatus(caseManagementOrderStatus.Update.MerchantReferenceNumber, caseManagementOrderStatus.Update.NewDecision, caseManagementOrderStatus.Update.ReviewerComments);
                            _context.Vtex.Logger.Info("DecisionManagerNotify", null, $"{caseManagementOrderStatus.Update.MerchantReferenceNumber} : {caseManagementOrderStatus.Update.OriginalDecision} - {caseManagementOrderStatus.Update.NewDecision}", new[] { ("result", result) });
                        }
                        catch (Exception ex)
                        {
                            _context.Vtex.Logger.Error("DecisionManagerNotify", null, "Error serializing request body", ex, new[] { ("body", bodyAsText) });
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Vtex.Logger.Error("DecisionManagerNotify", null, "Error decoding request body", ex, new[] { ("body", bodyAsTextRaw) });
                    }
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("DecisionManagerNotify", null, "Error reading request body", ex);
                }
            }

            return Ok();
        }

        public async Task<IActionResult> RetrieveTransaction(string requestId)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            return Json(await _cybersourcePaymentService.RetrieveTransaction(requestId));
        }

        public async Task<IActionResult> TestFlattenCustomData()
        {
            string response = null;
            PaymentRequestWrapper requestWrapper = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    try
                    {
                        VtexOrder vtexOrder = JsonConvert.DeserializeObject<VtexOrder>(bodyAsText);
                        CreatePaymentRequest createPaymentRequest = new CreatePaymentRequest();
                        requestWrapper = new PaymentRequestWrapper(createPaymentRequest);
                        response = requestWrapper.FlattenCustomData(vtexOrder.CustomData);
                        Console.WriteLine(response);
                    }
                    catch (Exception ex)
                    {
                        _context.Vtex.Logger.Error("TestFlattenCustomData", "FlattenCustomData", "Error", ex, new[] { ("body", bodyAsText) });
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("TestFlattenCustomData", "FlattenCustomData", "Error: ", ex);
            }

            return Json(requestWrapper);
        }

        public async Task<IActionResult> TestMerchantDefinedData()
        {
            List<MerchantDefinedInformation> mdd = null;

            try
            {
                if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                    CreatePaymentRequest createPaymentRequest = JsonConvert.DeserializeObject<CreatePaymentRequest>(bodyAsText);
                    PaymentRequestWrapper paymentRequestWrapper = new PaymentRequestWrapper(createPaymentRequest);
                    VtexOrder vtexOrder = await _vtexApiService.GetOrderInformation($"{createPaymentRequest.OrderId}");
                    if (vtexOrder != null)
                    {
                        if (vtexOrder.CustomData != null && vtexOrder.CustomData.CustomApps != null)
                        {
                            string response = paymentRequestWrapper.FlattenCustomData(vtexOrder.CustomData);
                            if (!string.IsNullOrWhiteSpace(response))
                            {
                                // A response indicates an error.
                                Console.WriteLine($"ERROR: {response}");
                            }
                        }

                        if (vtexOrder.MarketingData != null)
                        {
                            string response = paymentRequestWrapper.SetMarketingData(vtexOrder.MarketingData);
                            if (!string.IsNullOrWhiteSpace(response))
                            {
                                // A response indicates an error.
                                Console.WriteLine($"ERROR: {response}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"ERROR: {createPaymentRequest.OrderId} is NULL!");
                    }

                    mdd = await _cybersourcePaymentService.GetMerchantDefinedInformation(merchantSettings, paymentRequestWrapper);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("TestMerchantDefinedData", null, "Error: ", ex);
            }

            return Json(mdd);
        }

        static async Task<T> DelayedDummyResultTask<T>(TimeSpan delay)
        {
            await Task.Delay(delay);
            return default(T);
        }
    }
}
