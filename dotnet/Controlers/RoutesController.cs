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
    using System.Text;
    using System.Xml;
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
                _context.Vtex.Logger.Debug("CreatePayment", null, bodyAsText);
                //Console.WriteLine(bodyAsText);
                CreatePaymentRequest createPaymentRequest = JsonConvert.DeserializeObject<CreatePaymentRequest>(bodyAsText);
                paymentResponse = await this._cybersourcePaymentService.CreatePayment(createPaymentRequest);
            }

            Response.Headers.Add("Cache-Control", "private");

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
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                CancelPaymentRequest cancelPaymentRequest = JsonConvert.DeserializeObject<CancelPaymentRequest>(bodyAsText);
                cancelResponse = await this._cybersourcePaymentService.CancelPayment(cancelPaymentRequest);
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
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                CapturePaymentRequest capturePaymentRequest = JsonConvert.DeserializeObject<CapturePaymentRequest>(bodyAsText);
                captureResponse = await this._cybersourcePaymentService.CapturePayment(capturePaymentRequest);
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
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                RefundPaymentRequest refundPaymentRequest = JsonConvert.DeserializeObject<RefundPaymentRequest>(bodyAsText);
                refundResponse = await this._cybersourcePaymentService.RefundPayment(refundPaymentRequest);
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
            //Console.WriteLine(" ------- MANIFEST ----------- ");
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
                CustomFields = new List<CustomField>
                {
                    new CustomField
                    {
                        Name = "Company Name",
                        Type = "text"
                    },
                    new CustomField
                    {
                        Name = "Company Tax Id",
                        Type = "text"
                    }
                }
            };

            //Response.Headers.Add("Cache-Control", "private");

            return Json(manifest);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/transactions
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> SendAntifraudData()
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                SendAntifraudDataRequest sendAntifraudDataRequest = JsonConvert.DeserializeObject<SendAntifraudDataRequest>(bodyAsText);
                sendAntifraudDataResponse = await this._cybersourcePaymentService.SendAntifraudData(sendAntifraudDataRequest);

                //sendAntifraudDataResponse = new SendAntifraudDataResponse
                //{
                //    Id = sendAntifraudDataRequest.Id,
                //    Status = CybersourceConstants.VtexAntifraudStatus.Received
                //};
            }

            return Json(sendAntifraudDataResponse);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/pre-analysis
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> SendAntifraudPreAnalysisData()
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                SendAntifraudDataRequest sendAntifraudDataRequest = JsonConvert.DeserializeObject<SendAntifraudDataRequest>(bodyAsText);
                sendAntifraudDataResponse = await this._cybersourcePaymentService.SendAntifraudData(sendAntifraudDataRequest);
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

            getAntifraudStatusResponse = await this._cybersourcePaymentService.GetAntifraudStatus(transactionId);
            if (getAntifraudStatusResponse == null)
            {
                getAntifraudStatusResponse = new SendAntifraudDataResponse
                {
                    Id = transactionId,
                    Status = CybersourceConstants.VtexAntifraudStatus.Undefined
                };
            }

            return Json(getAntifraudStatusResponse);
        }

        public async Task<IActionResult> TaxHandler()
        {
            string orderFormId = string.Empty;
            string totalItems = string.Empty;
            bool fromCache = false;
            VtexTaxResponse vtexTaxResponse = new VtexTaxResponse
            {
                ItemTaxResponse = new List<ItemTaxResponse>()
            };

            Stopwatch timer = new Stopwatch();
            timer.Start();

            Response.Headers.Add("Cache-Control", "private");
            Response.Headers.Add(CybersourceConstants.CONTENT_TYPE, CybersourceConstants.MINICART);
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(bodyAsText))
                {
                    VtexTaxRequest taxRequest = JsonConvert.DeserializeObject<VtexTaxRequest>(bodyAsText);
                    orderFormId = taxRequest.OrderFormId;
                    totalItems = taxRequest.Items.Length.ToString();
                    decimal total = taxRequest.Totals.Sum(t => t.Value);
                    if (taxRequest != null)
                    {
                        int cacheKey = $"{_context.Vtex.App.Version}{taxRequest.ShippingDestination.PostalCode}{total}".GetHashCode();
                        if (_cybersourceRepository.TryGetCache(cacheKey, out vtexTaxResponse))
                        {
                            fromCache = true;
                            //_context.Vtex.Logger.Debug("CybersourceOrderTaxHandler", null, $"Taxes for '{cacheKey}' fetched from cache. {JsonConvert.SerializeObject(vtexTaxResponse)}");
                        }
                        else
                        {
                            vtexTaxResponse = await _vtexApiService.GetTaxes(taxRequest);
                            if (vtexTaxResponse != null)
                            {
                                await _cybersourceRepository.SetCache(cacheKey, vtexTaxResponse);
                            }
                        }
                    }
                }
            }

            timer.Stop();
            Console.WriteLine($"Elapsed Time = '{timer.Elapsed.TotalMilliseconds}' '{orderFormId}' {totalItems} items.  From cache? {fromCache}");
            _context.Vtex.Logger.Debug("TaxHandler", null, $"Elapsed Time = '{timer.Elapsed.TotalMilliseconds}' '{orderFormId}' {totalItems} items.  From cache? {fromCache}");

            return Json(vtexTaxResponse);
        }

        public async Task<IActionResult> Authorize()
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string url = await _cybersourcePaymentService.GetAuthUrl();
            if (string.IsNullOrEmpty(url))
            {
                return Json("Error");
            }
            else
            {
                return Redirect(url);
            }
        }

        public async Task<bool> SaveToken(bool isLive)
        {
            bool success = false;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                CybersourceToken token = JsonConvert.DeserializeObject<CybersourceToken>(bodyAsText);

                success = await _cybersourceRepository.SaveToken(token, isLive);
            }

            return success;
        }

        public async Task<IActionResult> ToggleTax(bool useCyberTax)
        {
            Response.Headers.Add("Cache-Control", "no-cache");
            string result = string.Empty;
            if(useCyberTax)
            {
                result = await _vtexApiService.InitConfiguration();
            }
            else
            {
                result = await _vtexApiService.RemoveConfiguration();
            }

            return Json(result);
        }

        public async Task<IActionResult> CybersourceResponseToVtexResponse()
        {
            VtexTaxResponse vtexTaxResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                PaymentsResponse taxResponse = JsonConvert.DeserializeObject<PaymentsResponse>(bodyAsText);
                vtexTaxResponse = await this._vtexApiService.CybersourceResponseToVtexResponse(taxResponse);
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
            string result = string.Empty;
            string bodyAsText = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            //_context.Vtex.Logger.Debug("DecisionManagerNotify", null, bodyAsText);
            bodyAsText = HttpUtility.UrlDecode(bodyAsText);
            bodyAsText = bodyAsText.Substring(bodyAsText.IndexOf("=") + 1);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(CaseManagementOrderStatus));
                CaseManagementOrderStatus caseManagementOrderStatus;

                using (TextReader reader = new StringReader(bodyAsText))
                {
                    caseManagementOrderStatus = (CaseManagementOrderStatus)serializer.Deserialize(reader);
                };

                //Console.WriteLine($"DecisionManagerNotify {caseManagementOrderStatus.Update.MerchantReferenceNumber} : {caseManagementOrderStatus.Update.OriginalDecision} - {caseManagementOrderStatus.Update.NewDecision} ");
                result = await _vtexApiService.UpdateOrderStatus(caseManagementOrderStatus.Update.MerchantReferenceNumber, caseManagementOrderStatus.Update.NewDecision, caseManagementOrderStatus.Update.ReviewerComments);
                _context.Vtex.Logger.Info("DecisionManagerNotify", null, result);
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DecisionManagerNotify Error: {ex.Message}");
                _context.Vtex.Logger.Error("DecisionManagerNotify", null, bodyAsText, ex);
            }

            return Ok();
        }
    }
}
