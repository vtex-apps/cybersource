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
            CreatePaymentResponse paymentResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                try
                {
                    CreatePaymentRequest createPaymentRequest = JsonConvert.DeserializeObject<CreatePaymentRequest>(bodyAsText);
                    MerchantSetting merchantSettingPayerAuth = createPaymentRequest.MerchantSettings.FirstOrDefault(s => s.Name.Equals(CybersourceConstants.ManifestCustomField.UsePayerAuth));
                    if (merchantSettingPayerAuth != null && merchantSettingPayerAuth.Value.Equals(CybersourceConstants.ManifestCustomField.Active))
                    {
                        paymentResponse = await this._cybersourcePaymentService.SetupPayerAuth(createPaymentRequest);
                    }
                    else
                    {
                        paymentResponse = await this._cybersourcePaymentService.CreatePayment(createPaymentRequest);
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

            return Json(paymentResponse);
        }

        public async Task<IActionResult> PayerAuth(string paymentId)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            CreatePaymentResponse paymentResponse = null;
            PaymentData paymentData = await _cybersourceRepository.GetPaymentData(paymentId);
            if(paymentData != null && paymentData.CreatePaymentRequest != null)
            {
                if (!string.IsNullOrEmpty(paymentData.PayerAuthReferenceId))
                {
                    Console.WriteLine($" -- paymentData.PayerAuthReferenceId == {paymentData.PayerAuthReferenceId} -- ");
                    Console.WriteLine($" -- [{paymentId}] == [{paymentData.CreatePaymentRequest.PaymentId}] -- ");
                    paymentResponse = await this._cybersourcePaymentService.CreatePayment(paymentData.CreatePaymentRequest);
                    _context.Vtex.Logger.Debug("PayerAuth", null, string.Empty, new[] 
                    {
                        ("paymentId", paymentId),
                        ("paymentResponse", JsonConvert.SerializeObject(paymentResponse))
                    });
                }
                else
                {
                    paymentResponse = new CreatePaymentResponse
                    {
                        Message = "Missing PayerAuthReferenceId"
                    };

                    _context.Vtex.Logger.Debug("PayerAuth", null, "Missing PayerAuthReferenceId");
                }
            }

            return Json(paymentResponse);
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
                    _context.Vtex.Logger.Debug("CapturePayment", "bodyAsText", bodyAsText);
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
                    _context.Vtex.Logger.Debug("RefundPayment", "bodyAsText", bodyAsText);
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

            //Response.Headers.Add("Cache-Control", "private");

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
                if(useCyberTax)
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
            //ActionResult actionResult = BadRequest();
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string result = string.Empty;
                try
                {
                    string bodyAsTextRaw = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                    try
                    {
                        string bodyAsText = HttpUtility.UrlDecode(bodyAsTextRaw);
                        //_context.Vtex.Logger.Debug("DecisionManagerNotify", null, "Request.Body", new[] { ("body", bodyAsTextRaw) });
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
                            //actionResult = Ok();
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

            //return actionResult;
            return Ok();
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
    }
}
