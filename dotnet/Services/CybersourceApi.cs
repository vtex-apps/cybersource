using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Cybersource.Data;
using Cybersource.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Vtex.Api.Context;

namespace Cybersource.Services
{
    public class CybersourceApi : ICybersourceApi
    {
        private readonly IIOServiceContext _context;
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ICybersourceRepository _cybersourceRepository;
        private readonly string _applicationName;

        public CybersourceApi(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ICybersourceRepository cybersourceRepository)
        {
            this._context = context ??
                            throw new ArgumentNullException(nameof(context));

            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._cybersourceRepository = cybersourceRepository ??
                               throw new ArgumentNullException(nameof(cybersourceRepository));

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        public async Task<SendResponse> SendRequest(HttpMethod method, string endpoint, string jsonSerializedData)
        {
            SendResponse sendResponse = null;
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string urlBase = CybersourceConstants.ProductionApiEndpoint;
            if(!merchantSettings.IsLive)
            {
                urlBase = CybersourceConstants.SandboxApiEndpoint;
            }

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri($"https://{urlBase}{endpoint}")
                };

                if(!string.IsNullOrEmpty(jsonSerializedData))
                {
                    request.Content = new StringContent(jsonSerializedData, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON);
                }

                string digest = string.Empty;
                string signatureString = string.Empty;
                string signature = string.Empty;
                string gmtDateTime = DateTime.UtcNow.ToString("r");

                request.Headers.Add("v-c-merchant-id", merchantSettings.MerchantId);
                request.Headers.Add("Date", gmtDateTime);
                request.Headers.Add("Host", urlBase);
                if(!method.Equals(HttpMethod.Get) && !method.Equals(HttpMethod.Delete))
                {
                    digest = await this.GenerateDigest(jsonSerializedData);
                    request.Headers.Add("Digest", digest); // Do not pass this header field for GET requests. It is a hash of the JSON payload made using a SHA-256 hashing algorithm.
                }

                signatureString = await this.GenerateSignatureString(merchantSettings, urlBase, gmtDateTime, $"{method.ToString().ToLower()} {endpoint}", digest);
                //signature = await this.GenerateSignatureFromParams(signatureString, merchantSettings.SharedSecretKey);
                request.Headers.Add("Signature", signatureString);  // A comma-separated list of parameters that are formatted as name-value pairs

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                sendResponse = new SendResponse
                {
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode,
                    Message = responseContent
                };

                Console.WriteLine($"- SendRequest: [{response.StatusCode}] - ");

                StringBuilder sb = new StringBuilder();
                foreach (var header in request.Headers)
                {
                    string headerName = header.Key;
                    string headerContent = string.Join(",", header.Value.ToArray());
                    sb.AppendLine($"{headerName} : {headerContent}");
                    //Console.WriteLine($" |-| {headerName} : {headerContent}");
                }

                _context.Vtex.Logger.Debug("SendRequest", null, $"{request.RequestUri}\n{sb}\n{jsonSerializedData}\n\n[{response.StatusCode}]\n{responseContent}");
                //Console.WriteLine($"- SendRequest: [{response.StatusCode}] {responseContent}");
                //Console.WriteLine($"v-c-merchant-id: {merchantSettings.MerchantId}\nDate: {gmtDateTime}\nHost: {urlBase}\nDigest: {digest}\nSignature: {signatureString}");

                //_context.Vtex.Logger.Debug("SendRequest", null, $"{request.RequestUri}\n{jsonSerializedData}\nv-c-merchant-id: {merchantSettings.MerchantId}\nDate: {gmtDateTime}\nHost: {urlBase}\nDigest: {digest}\nSignature: {signatureString}\n[{response.StatusCode}]\n{responseContent}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendRequest", null, $"Error ", ex);
            }

            return sendResponse;
        }

        public async Task<SendResponse> SendReportRequest(HttpMethod method, string endpoint, string jsonSerializedData)
        {
            SendResponse sendResponse = null;
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string urlBase = CybersourceConstants.ProductionApiEndpoint;
            if (!merchantSettings.IsLive)
            {
                urlBase = CybersourceConstants.SandboxApiEndpoint;
            }

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri($"https://{urlBase}{endpoint}")
                };

                if (!string.IsNullOrEmpty(jsonSerializedData))
                {
                    request.Content = new StringContent(jsonSerializedData, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON);
                }

                string digest = string.Empty;
                string signatureString = string.Empty;
                string signature = string.Empty;
                string gmtDateTime = DateTime.UtcNow.ToString("r");

                request.Headers.Add("v-c-merchant-id", merchantSettings.MerchantId);
                request.Headers.Add("v-c-date", gmtDateTime);
                request.Headers.Add("host", urlBase);

                signatureString = await this.GenerateReportSignatureString(merchantSettings, urlBase, gmtDateTime, $"{method.ToString().ToLower()} {endpoint}", digest);
                request.Headers.Add("signature", signatureString);  // A comma-separated list of parameters that are formatted as name-value pairs
                request.Headers.Add("accept", "application/hal+json");

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                sendResponse = new SendResponse
                {
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode,
                    Message = responseContent
                };

                Console.WriteLine($"- SendReportRequest: [{response.StatusCode}] {responseContent} -");

                _context.Vtex.Logger.Debug("SendReportRequest", null, $"{request.RequestUri}\n{jsonSerializedData}\nv-c-merchant-id: {merchantSettings.MerchantId}\nDate: {gmtDateTime}\nHost: {urlBase}\nDigest: {digest}\nSignature: {signatureString}\n[{response.StatusCode}]\n{responseContent}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendReportRequest", null, $"Error ", ex);
            }

            return sendResponse;
        }

        public async Task<SendResponse> SendProxyRequest(HttpMethod method, string endpoint, string jsonSerializedData, string proxyUrl, string proxyTokenUrl)
        {
            SendResponse sendResponse = null;
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //CybersourceToken token = await this.GetOAuthToken(merchantSettings.IsLive);
            string urlBase = CybersourceConstants.ProductionApiEndpoint;
            if (!merchantSettings.IsLive)
            {
                urlBase = CybersourceConstants.SandboxApiEndpoint;
            }

            string requestUri = $"https://{urlBase}{endpoint}";

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri(proxyUrl)
                };

                if (!string.IsNullOrEmpty(jsonSerializedData))
                {
                    request.Content = new StringContent(jsonSerializedData, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON);
                }

                string digest = string.Empty;
                string signatureString = string.Empty;
                string signature = string.Empty;
                string gmtDateTime = DateTime.UtcNow.ToString("r");
                request.Headers.Add($"{CybersourceConstants.PROXY_HEADER_PREFIX}v-c-merchant-id", merchantSettings.MerchantId);
                request.Headers.Add($"{CybersourceConstants.PROXY_HEADER_PREFIX}Date", gmtDateTime);
                request.Headers.Add($"{CybersourceConstants.PROXY_HEADER_PREFIX}Host", urlBase);
                if (!method.Equals(HttpMethod.Get) && !method.Equals(HttpMethod.Delete))
                {
                    SendResponse proxyTokenSendResponse = await this.SendProxyDigestRequest(jsonSerializedData, proxyTokenUrl);
                    if (proxyTokenSendResponse.Success)
                    {
                        ProxyTokenResponse proxyToken = JsonConvert.DeserializeObject<ProxyTokenResponse>(proxyTokenSendResponse.Message);
                        digest = proxyToken.Tokens[0].Placeholder;
                    }
                    else
                    {
                        Console.WriteLine("Did not calculate digest!");
                    }

                    /// TESTING ------------------------------------------
                    //Console.WriteLine("     !!!!    OVERRIDING CARD DATA FOR TESTING    !!!!    ");
                    //digest = await this.GenerateDigest(jsonSerializedData);
                    /// TESTING ------------------------------------------

                    request.Headers.Add($"{CybersourceConstants.PROXY_HEADER_PREFIX}Digest", digest); // Do not pass this header field for GET requests. It is a hash of the JSON payload made using a SHA-256 hashing algorithm.
                }

                signatureString = await this.GenerateProxySignatureString(merchantSettings, urlBase, gmtDateTime, $"{method.ToString().ToLower()} {endpoint}", digest, proxyTokenUrl);
                request.Headers.Add($"{CybersourceConstants.PROXY_HEADER_PREFIX}Signature", signatureString);  // A comma-separated list of parameters that are formatted as name-value pairs

                request.Headers.Add(CybersourceConstants.PROXY_FORWARD_TO, requestUri);

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
                sendResponse = new SendResponse
                {
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode,
                    Message = responseContent
                };

                Console.WriteLine($"- SendRequest: [{response.StatusCode}] - ");
                StringBuilder sb = new StringBuilder();
                foreach (var header in request.Headers)
                {
                    string headerName = header.Key;
                    string headerContent = string.Join(",", header.Value.ToArray());
                    sb.AppendLine($"{headerName} : {headerContent}");
                    //Console.WriteLine($" |-| {headerName} : {headerContent}");
                }

                _context.Vtex.Logger.Debug("SendRequest", "Proxy", $"{request.RequestUri}\n{sb}\n{jsonSerializedData}\n\n[{response.StatusCode}]\n{responseContent}");
                //_context.Vtex.Logger.Debug("SendRequest", "Proxy", $"{request.RequestUri}\n{jsonSerializedData}\n\n[{response.StatusCode}]\n{responseContent}");
                //_context.Vtex.Logger.Debug("SendRequest", "Proxy", $"{request.RequestUri}\n[{response.StatusCode}]\n{responseContent}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendRequest", "Proxy", $"Error ", ex);
            }

            return sendResponse;
        }

        public async Task<SendResponse> SendProxyDigestRequest(string jsonSerializedData, string proxyTokenUrl)
        {
            SendResponse sendResponse = null;
            ProxyTokenRequest proxyTokenRequest = new ProxyTokenRequest
            {
                Tokens = new RequestToken[]
                {
                    new RequestToken
                    {
                        Name = "digest",
                        Value = new Value
                        {
                            Sha256 = new Sha256
                            {
                                ReplaceTokens = new string[]
                                {
                                    jsonSerializedData
                                }
                            }
                        }
                    }
                }
            };

            sendResponse = await this.SendProxyTokenRequest(proxyTokenRequest, proxyTokenUrl);

            return sendResponse;
        }

        public async Task<SendResponse> SendProxySignatureRequest(string jsonSerializedData, string proxyTokenUrl, string key)
        {
            SendResponse sendResponse = null;
            ProxyTokenRequest proxyTokenRequest = new ProxyTokenRequest
            {
                Tokens = new RequestToken[]
                {
                    new RequestToken
                    {
                        Name = "signature",
                        Value = new Value
                        {
                            HmacSha256 = new HmacSha256
                            {
                                Key = new string[] { key },
                                Data = new string[] { jsonSerializedData }
                            }
                        }
                    }
                }
            };

            sendResponse = await this.SendProxyTokenRequest(proxyTokenRequest, proxyTokenUrl);

            return sendResponse;
        }

        public async Task<SendResponse> SendProxyTokenRequest(ProxyTokenRequest proxyTokenRequest, string proxyTokenUrl)
        {
            SendResponse sendResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(proxyTokenUrl)
                };

                request.Content = new StringContent(JsonConvert.SerializeObject(proxyTokenRequest), Encoding.UTF8, CybersourceConstants.APPLICATION_JSON);

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                sendResponse = new SendResponse
                {
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode,
                    Message = responseContent
                };

                Console.WriteLine($"- SendProxyTokenRequest: [{response.StatusCode}] - ");

                _context.Vtex.Logger.Debug("SendProxyTokenRequest", null, $"{request.RequestUri}\n{JsonConvert.SerializeObject(proxyTokenRequest)}\n[{response.StatusCode}]\n{responseContent}");
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendProxyTokenRequest", null, $"Error ", ex);
            }

            return sendResponse;
        }

        #region Payments
        public async Task<PaymentsResponse> ProcessPayment(Payments payments, string proxyUrl, string proxyTokensUrl)
        {
            PaymentsResponse paymentsResponse = null;
            /// TESTING ------------------------------------------
            //Console.WriteLine("     !!!!    OVERRIDING CARD DATA FOR TESTING    !!!!    ");
            //payments.paymentInformation.card.number = "4111111111111111";
            //payments.paymentInformation.card.number = "6362970000457013";
            //payments.paymentInformation.card.securityCode = "111";
            //payments.orderInformation.amountDetails.totalAmount = "401";
            //payments.orderInformation.billTo.postalCode = "28650";
            /// TESTING ------------------------------------------

            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}payments";
            SendResponse response = await this.SendProxyRequest(HttpMethod.Post, endpoint, json, proxyUrl, proxyTokensUrl);
            

            //SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            //response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            
            if (response != null)
            {
                if (response.Success)
                {
                    paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
                }
                else
                {
                    Console.WriteLine($"    ProcessPayment: {response.Message}");
                }
            }

            return paymentsResponse;
        }

        public async Task<PaymentsResponse> ProcessReversal(Payments payments, string paymentId)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}payments/{paymentId}/reversals";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }

        public async Task<PaymentsResponse> ProcessCapture(Payments payments, string paymentId)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}payments/{paymentId}/captures";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }

        /// Refund a Payment API is only used, if you have requested Authorization and Capture together
        public async Task<PaymentsResponse> RefundPayment(Payments payments, string paymentId)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}payments/{paymentId}/refunds";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }

        /// Refund a capture API is only used, if you have requested Capture independenlty
        public async Task<PaymentsResponse> RefundCapture(Payments payments, string captureId)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}captures/{captureId}/refunds";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }

        public async Task<PaymentsResponse> ProcessCredit(Payments payments)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.PAYMENTS}credits";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }
        #endregion

        #region Risk Management
        public async Task<PaymentsResponse> CreateDecisionManager(Payments payments)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.RISK}decisions";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if(response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }
        #endregion

        #region Tax
        public async Task<PaymentsResponse> CalculateTaxes(Payments payments)
        {
            PaymentsResponse paymentsResponse = null;
            string json = JsonConvert.SerializeObject(payments);
            string endpoint = $"{CybersourceConstants.TAX}tax";
            SendResponse response = await this.SendRequest(HttpMethod.Post, endpoint, json);
            if (response != null)
            {
                paymentsResponse = JsonConvert.DeserializeObject<PaymentsResponse>(response.Message);
            }

            return paymentsResponse;
        }
        #endregion

        #region Reporting
        /// <summary>
        /// The Conversion Detail Report contains details of transactions for a merchant.
        /// To request the report, your client application must send an HTTP GET message to the report server.
        /// The default format for responses is JSON, but some reports can also return CSV or XML.
        /// You can set the response to return CSV or XML in the request header by setting the Accept value to either application/xml or text/csv.
        /// </summary>
        /// <returns></returns>
        public async Task<ConversionReportResponse> ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime)
        {
            // https://<url_prefix>/reporting/v3/conversion-details?startTime={startTime}&endTime={endTime}&organizationId={organizationId}
            // 2016-11-22T12:00:00.000Z
            ConversionReportResponse retval = null;
            string startTime = dtStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string endTime = dtEndTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            //string startTime = dtStartTime.ToString("yyyy-MM-ddT00:00:00.000Z");
            //string endTime = dtEndTime.ToString("yyyy-MM-ddT23:00:00.000Z");
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string organizationId = merchantSettings.MerchantId;
            string endpoint = $"{CybersourceConstants.REPORTING}conversion-details?startTime={startTime}&endTime={endTime}&organizationId={organizationId}&";
            SendResponse response = await this.SendReportRequest(HttpMethod.Get, endpoint, null);
            if (response != null)
            {
                if(response.Success)
                {
                    retval = JsonConvert.DeserializeObject<ConversionReportResponse>(response.Message);
                }
                else
                {
                    ReportErrorResponse reportErrorResponse = JsonConvert.DeserializeObject<ReportErrorResponse>(response.Message);
                    Console.WriteLine(reportErrorResponse.Message);
                }
            }

            return retval;
        }

        /// <summary>
        /// Download the Notification of Change report.
        /// This report shows eCheck-related fields updated as a result of a response to an eCheck settlement transaction.
        /// </summary>
        /// <param name="dtStartTime"></param>
        /// <param name="dtEndTime"></param>
        /// <returns></returns>
        public async Task<string> NotificationOfChangesReport(DateTime dtStartTime, DateTime dtEndTime)
        {
            string retval = string.Empty;
            string startTime = dtStartTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            string endTime = dtEndTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            string endpoint = $"{CybersourceConstants.REPORTING}notification-of-changes&startTime={startTime}&endTime={endTime}";
            SendResponse response = await this.SendRequest(HttpMethod.Get, endpoint, null);
            if (response != null)
            {
                //= JsonConvert.DeserializeObject<>(response.Message);
                Console.WriteLine($"[{response.StatusCode}] {response.Message}");
                retval = $"[{response.StatusCode}] {response.Message}";
            }

            return retval;
        }

        /// <summary>
        /// Download a report using the unique report name and date.
        /// </summary>
        /// <param name="dtReportDate"></param>
        /// <param name="reportName"></param>
        /// <returns></returns>
        public async Task<string> DownloadReport(DateTime dtReportDate, string reportName)
        {
            string retval = string.Empty;
            string reportDate = dtReportDate.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            string organizationId = merchantSettings.MerchantId;
            string endpoint = $"{CybersourceConstants.REPORTING}report-downloads?reportDate={reportDate}&organizationId={organizationId}&reportName={reportName}";
            SendResponse response = await this.SendRequest(HttpMethod.Get, endpoint, null);
            if (response != null)
            {
                //= JsonConvert.DeserializeObject<>(response.Message);
                Console.WriteLine($"[{response.StatusCode}] {response.Message}");
                retval = $"[{response.StatusCode}] {response.Message}";
            }

            return retval;
        }

        /// <summary>
        /// Retrieve a list of the available reports to which you are subscribed.
        /// This will also give you the reportId value, which you can also use to download a report.
        /// </summary>
        /// <param name="dtStartTime"></param>
        /// <param name="dtEndTime"></param>
        /// <returns></returns>
        public async Task<string> RetrieveAvailableReports(DateTime dtStartTime, DateTime dtEndTime)
        {
            string retval = string.Empty;
            string startTime = dtStartTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            string endTime = dtEndTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            //MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //string organizationId = merchantSettings.MerchantId;
            //string endpoint = $"{CybersourceConstants.REPORTING}reports&startTime={startTime}&endTime={endTime}&organizationId={organizationId}";
            string endpoint = $"{CybersourceConstants.REPORTING}reports?startTime={startTime}&endTime={endTime}&timeQueryType=executedTime";
            SendResponse response = await this.SendRequest(HttpMethod.Get, endpoint, null);
            if (response != null)
            {
                //= JsonConvert.DeserializeObject<>(response.Message);
                Console.WriteLine($"[{response.StatusCode}] {response.Message}");
                retval = $"[{response.StatusCode}] {response.Message}";
            }

            return retval;
        }

        public async Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime)
        {
            string retval = string.Empty;
            string startTime = dtStartTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            string endTime = dtEndTime.ToString("yyyy-MM-ddTHH:mm:ss.sssZ");
            //MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //string organizationId = merchantSettings.MerchantId;
            string endpoint = $"{CybersourceConstants.REPORTING}purchase-refund-details?startTime={startTime}&endTime={endTime}";
            SendResponse response = await this.SendRequest(HttpMethod.Get, endpoint, null);
            if (response != null)
            {
                //= JsonConvert.DeserializeObject<>(response.Message);
                Console.WriteLine($"[{response.StatusCode}] {response.Message}");
                retval = $"[{response.StatusCode}] {response.Message}";
            }

            return retval;
        }
        #endregion Reporting

        #region Authorization Header functions
        private async Task<string> GenerateDigest(string jsonPayload)
        {
            string digest = string.Empty;
            using (var sha256hash = SHA256.Create())
            {
                byte[] payloadBytes = sha256hash.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
                digest = Convert.ToBase64String(payloadBytes);
                digest = "SHA-256=" + digest;
            }
            
            return digest;
        }

        private async Task<string> GenerateSignatureFromParams(string signatureParams, string secretKey)
        {
            var sigBytes = Encoding.UTF8.GetBytes(signatureParams);
            var decodedSecret = Convert.FromBase64String(secretKey);
            var hmacSha256 = new HMACSHA256(decodedSecret);
            var messageHash = hmacSha256.ComputeHash(sigBytes);
            return Convert.ToBase64String(messageHash);
        }

        private async Task<string> GenerateSignatureString(MerchantSettings merchantSettings, string hostName, string gmtDateTime, string requestTarget, string digest)
        {
            var signatureString = new StringBuilder();
            var signatureHeaderValue = new StringBuilder();
            string headersString = string.Empty;
            const string getOrDeleteHeaders = "host date (request-target) v-c-merchant-id";
            const string postOrPutHeaders = "host date (request-target) digest v-c-merchant-id";
            if(string.IsNullOrEmpty(digest))
            {
                headersString = getOrDeleteHeaders;
            }
            else
            {
                headersString = postOrPutHeaders;
            }

            signatureString.Append('\n');
            signatureString.Append("host");
            signatureString.Append(": ");
            signatureString.Append(hostName);
            signatureString.Append('\n');
            signatureString.Append("date");
            signatureString.Append(": ");
            signatureString.Append(gmtDateTime);
            signatureString.Append('\n');
            signatureString.Append("(request-target)");
            signatureString.Append(": ");
            signatureString.Append(requestTarget);
            signatureString.Append('\n');
            if(!string.IsNullOrEmpty(digest))
            {
                signatureString.Append("digest");
                signatureString.Append(": ");
                signatureString.Append(digest);
                signatureString.Append('\n');
            }

            signatureString.Append("v-c-merchant-id");
            signatureString.Append(": ");
            signatureString.Append(merchantSettings.MerchantId);
            signatureString.Remove(0, 1);

            var signatureByteString = Encoding.UTF8.GetBytes(signatureString.ToString());
            var decodedKey = Convert.FromBase64String(merchantSettings.SharedSecretKey);
            var aKeyId = new HMACSHA256(decodedKey);
            var hashmessage = aKeyId.ComputeHash(signatureByteString);
            var base64EncodedSignature = Convert.ToBase64String(hashmessage);

            signatureHeaderValue.Append("keyid=\"" + merchantSettings.MerchantKey + "\"");
            signatureHeaderValue.Append(", algorithm=\"" + CybersourceConstants.SignatureAlgorithm + "\"");
            signatureHeaderValue.Append(", headers=\"" + headersString + "\"");
            signatureHeaderValue.Append(", signature=\"" + base64EncodedSignature + "\"");

            //_context.Vtex.Logger.Debug("GenerateSignatureString", null, signatureString.ToString());
            Console.WriteLine(signatureString.ToString());

            return signatureHeaderValue.ToString();
        }

        private async Task<string> GenerateProxySignatureString(MerchantSettings merchantSettings, string hostName, string gmtDateTime, string requestTarget, string digest, string proxyTokenUrl)
        {
            var signatureString = new StringBuilder();
            var signatureHeaderValue = new StringBuilder();
            string headersString = string.Empty;
            const string getOrDeleteHeaders = "host date (request-target) v-c-merchant-id";
            const string postOrPutHeaders = "host date (request-target) digest v-c-merchant-id";
            if (string.IsNullOrEmpty(digest))
            {
                headersString = getOrDeleteHeaders;
            }
            else
            {
                headersString = postOrPutHeaders;
            }

            signatureString.Append('\n');
            signatureString.Append("host");
            signatureString.Append(": ");
            signatureString.Append(hostName);
            signatureString.Append('\n');
            signatureString.Append("date");
            signatureString.Append(": ");
            signatureString.Append(gmtDateTime);
            signatureString.Append('\n');
            signatureString.Append("(request-target)");
            signatureString.Append(": ");
            signatureString.Append(requestTarget);
            signatureString.Append('\n');
            if (!string.IsNullOrEmpty(digest))
            {
                signatureString.Append("digest");
                signatureString.Append(": ");
                signatureString.Append(digest);
                signatureString.Append('\n');
            }

            signatureString.Append("v-c-merchant-id");
            signatureString.Append(": ");
            signatureString.Append(merchantSettings.MerchantId);
            signatureString.Remove(0, 1);

            //var signatureByteString = Encoding.UTF8.GetBytes(signatureString.ToString());
            //var decodedKey = Convert.FromBase64String(merchantSettings.SharedSecretKey);
            //var aKeyId = new HMACSHA256(decodedKey);
            //var hashmessage = aKeyId.ComputeHash(signatureByteString);
            //var base64EncodedSignature = Convert.ToBase64String(hashmessage);
            string base64EncodedSignature = string.Empty;
            SendResponse proxyTokenSendResponse = await this.SendProxySignatureRequest(signatureString.ToString(), proxyTokenUrl, merchantSettings.SharedSecretKey);
            if (proxyTokenSendResponse.Success)
            {
                ProxyTokenResponse proxyToken = JsonConvert.DeserializeObject<ProxyTokenResponse>(proxyTokenSendResponse.Message);
                base64EncodedSignature = proxyToken.Tokens[0].Placeholder;
            }
            else
            {
                Console.WriteLine("Did not calculate signature!");
            }

            signatureHeaderValue.Append("keyid=\"" + merchantSettings.MerchantKey + "\"");
            signatureHeaderValue.Append(", algorithm=\"" + CybersourceConstants.SignatureAlgorithm + "\"");
            signatureHeaderValue.Append(", headers=\"" + headersString + "\"");
            signatureHeaderValue.Append(", signature=\"" + base64EncodedSignature + "\"");

            _context.Vtex.Logger.Debug("GenerateProxySignatureString", null, signatureString.ToString());
            Console.WriteLine(signatureString.ToString());

            return signatureHeaderValue.ToString();
        }

        private async Task<string> GenerateReportSignatureString(MerchantSettings merchantSettings, string hostName, string gmtDateTime, string requestTarget, string digest)
        {
            var signatureString = new StringBuilder();
            var signatureHeaderValue = new StringBuilder();
            string headersString = "host v-c-date (request-target) v-c-merchant-id";

            signatureString.Append('\n');
            signatureString.Append("host");
            signatureString.Append(": ");
            signatureString.Append(hostName);
            signatureString.Append('\n');
            signatureString.Append("v-c-date");
            signatureString.Append(": ");
            signatureString.Append(gmtDateTime);
            signatureString.Append('\n');
            signatureString.Append("(request-target)");
            signatureString.Append(": ");
            signatureString.Append(requestTarget);
            signatureString.Append('\n');
            signatureString.Append("v-c-merchant-id");
            signatureString.Append(": ");
            signatureString.Append(merchantSettings.MerchantId);
            signatureString.Remove(0, 1);

            var signatureByteString = Encoding.UTF8.GetBytes(signatureString.ToString());
            var decodedKey = Convert.FromBase64String(merchantSettings.SharedSecretKey);
            var aKeyId = new HMACSHA256(decodedKey);
            var hashmessage = aKeyId.ComputeHash(signatureByteString);
            var base64EncodedSignature = Convert.ToBase64String(hashmessage);

            signatureHeaderValue.Append("keyid=\"" + merchantSettings.MerchantKey + "\"");
            signatureHeaderValue.Append(", algorithm=\"" + CybersourceConstants.SignatureAlgorithm + "\"");
            signatureHeaderValue.Append(", headers=\"" + headersString + "\"");
            signatureHeaderValue.Append(", signature=\"" + base64EncodedSignature + "\"");

            //_context.Vtex.Logger.Debug("GenerateSignatureString", null, signatureString.ToString());
            //Console.WriteLine(signatureHeaderValue.ToString());

            return signatureHeaderValue.ToString();
        }
        #endregion

        #region OAuth
        public async Task<CybersourceToken> GetOAuthToken(bool isProduction)
        {
            CybersourceToken token = await _cybersourceRepository.LoadToken(isProduction);
            if (token != null && !string.IsNullOrEmpty(token.RefreshToken))
            {
                string refreshToken = token.RefreshToken;
                if (token != null) // && !string.IsNullOrEmpty(token.AccessToken))
                {
                    if (token.ExpiresAt <= DateTime.Now)
                    {
                        token = await this.RefreshToken(refreshToken, isProduction);
                        if (token != null)
                        {
                            token.ExpiresAt = DateTime.Now.AddSeconds(token.ExpiresIn);
                            if (string.IsNullOrEmpty(token.RefreshToken))
                            {
                                token.RefreshToken = refreshToken;
                            }

                            bool saved = await _cybersourceRepository.SaveToken(token, isProduction);
                        }
                        else
                        {
                            _context.Vtex.Logger.Warn("GetOAuthToken", null, $"Could not refresh token.");
                        }
                    }
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetOAuthToken", null, $"Could not load token. Have Access token?{!string.IsNullOrEmpty(token.AccessToken)} Have Refresh token?{!string.IsNullOrEmpty(token.RefreshToken)}");
                    token = null;
                }
            }
            else
            {
                _context.Vtex.Logger.Warn("GetOAuthToken", null, $"Could not load token.  Refresh token was null. Have Access token?{token != null && !string.IsNullOrEmpty(token.AccessToken)}");
            }

            return token;
        }

        private async Task<CybersourceToken> RefreshToken(string refreshToken, bool isProduction)
        {
            CybersourceToken token = null;
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{CybersourceConstants.AUTH_SITE_BASE}/{CybersourceConstants.AUTH_APP_PATH}/{CybersourceConstants.REFRESH_PATH}/{isProduction}/{HttpUtility.UrlEncode(refreshToken)}"),
                    Content = new StringContent(string.Empty, Encoding.UTF8, CybersourceConstants.APPLICATION_FORM)
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                try
                {
                    var response = await client.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        token = JsonConvert.DeserializeObject<CybersourceToken>(responseContent);
                    }
                    else
                    {
                        _context.Vtex.Logger.Info("RefreshToken", null, $"{response.StatusCode} {responseContent}");
                    }
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("RefreshToken", null, $"Refresh Token {refreshToken}", ex);
                }
            }

            return token;
        }
        #endregion OAuth
    }
}