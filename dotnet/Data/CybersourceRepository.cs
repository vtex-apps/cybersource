namespace Cybersource.Data
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Cybersource.Models;
    using Cybersource.Services;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Vtex.Api.Context;

    public class CybersourceRepository : ICybersourceRepository
    {
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IIOServiceContext _context;
        private readonly ICachedKeys _cachedKeys;
        private readonly string _applicationName;

        public CybersourceRepository(IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, IIOServiceContext context, ICachedKeys cachedKeys)
        {
            this._environmentVariableProvider = environmentVariableProvider ??
                                                throw new ArgumentNullException(nameof(environmentVariableProvider));

            this._httpContextAccessor = httpContextAccessor ??
                                        throw new ArgumentNullException(nameof(httpContextAccessor));

            this._clientFactory = clientFactory ??
                               throw new ArgumentNullException(nameof(clientFactory));

            this._context = context ??
                               throw new ArgumentNullException(nameof(context));

            this._cachedKeys = cachedKeys ??
                               throw new ArgumentNullException(nameof(cachedKeys));

            this._applicationName =
                $"{this._environmentVariableProvider.ApplicationVendor}.{this._environmentVariableProvider.ApplicationName}";
        }

        public async Task<MerchantSettings> GetMerchantSettings()
        {
            MerchantSettings merchantSettings = null;
            // Load merchant settings
            // 'http://apps.{{region}}.vtex.io/{{account}}/{{workspace}}/apps/{{appName}}/settings'
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Get,
                $"http://apps.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/apps/{CybersourceConstants.APP_SETTINGS}/settings",
                null);
            if (sendResponse.Success)
            {
                merchantSettings = JsonConvert.DeserializeObject<MerchantSettings>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Warn("GetMerchantSettings", null, $"Failed. {sendResponse.StatusCode} {sendResponse.Message}");
            }

            return merchantSettings;
        }

        public async Task<bool> SetMerchantSettings(MerchantSettings merchantSettings)
        {
            bool isSuccess = false;
            var jsonSerializedMerchantSettings = JsonConvert.SerializeObject(merchantSettings);
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Put,
                $"http://apps.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/apps/{CybersourceConstants.APP_SETTINGS}/settings",
                jsonSerializedMerchantSettings);
            if (sendResponse.Success)
            {
                isSuccess = true;
            }
            else
            {
                _context.Vtex.Logger.Warn("SetMerchantSettings", null, $"Failed. {sendResponse.StatusCode} {sendResponse.Message}");
            }

            return isSuccess;
        }

        public async Task<PaymentData> GetPaymentData(string paymentIdentifier)
        {
            PaymentData paymentData =  null;

            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Get,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT}/files/{paymentIdentifier}",
                null);
            if (sendResponse.StatusCode == HttpStatusCode.NotFound.ToString())
            {
                _context.Vtex.Logger.Warn("GetPaymentData", null, $"Payment Id '{paymentIdentifier}' Not Found.");
                return null;
            }

            try
            {
                paymentData = JsonConvert.DeserializeObject<PaymentData>(sendResponse.Message);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetPaymentData", null,
                "Error:", ex,
                new[]
                {
                    ( "paymentIdentifier", paymentIdentifier )
                });
            }

            return paymentData;
        }

        public async Task SavePaymentData(string paymentIdentifier, PaymentData paymentData)
        {
            if (paymentData == null)
            {
                paymentData = new PaymentData();
            }

            var jsonSerializedCreatePaymentRequest = JsonConvert.SerializeObject(paymentData);
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Put,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT}/files/{paymentIdentifier}",
                jsonSerializedCreatePaymentRequest);
            if (!sendResponse.Success)
            {
                _context.Vtex.Logger.Error("SavePaymentData", null, $"Failed",
                    null,
                    new[]
                {
                        ( "StatusCode", sendResponse.StatusCode ),
                        ( "Message", sendResponse.Message ),
                        ( "paymentIdentifier", paymentIdentifier ),
                        ( "paymentData", JsonConvert.SerializeObject(paymentData) )
                });
            }
            //else
            //{
            //    _context.Vtex.Logger.Info("SavePaymentData", null, "Saved.",
            //        new[]
            //    {
            //            ( "paymentIdentifier", paymentIdentifier ),
            //            ( "paymentData", JsonConvert.SerializeObject(paymentData) )
            //    });
            //}
        }

        public async Task<SendAntifraudDataResponse> GetAntifraudData(string id)
        {
            SendAntifraudDataResponse antifraudDataResponse = null;
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Get,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_ANTIFRAUD}/files/{id}",
                null);
            if (sendResponse.StatusCode == HttpStatusCode.NotFound.ToString())
            {
                _context.Vtex.Logger.Warn("GetAntifraudData", null, "Not Found.", new[] { ( "id", id ) });
                return null;
            }
            else
            {
                try
                {
                    antifraudDataResponse = JsonConvert.DeserializeObject<SendAntifraudDataResponse>(sendResponse.Message);
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("GetAntifraudData", null,
                    "Error:", ex,
                    new[]
                    {
                    ( "id", id )
                    });
                }
            }

            return antifraudDataResponse;
        }

        public async Task SaveAntifraudData(string id, SendAntifraudDataResponse antifraudDataResponse)
        {
            if (antifraudDataResponse == null)
            {
                antifraudDataResponse = new SendAntifraudDataResponse();
            }

            var jsonSerializedAntifraudDataResponse = JsonConvert.SerializeObject(antifraudDataResponse);
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Put,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_ANTIFRAUD}/files/{id}",
                jsonSerializedAntifraudDataResponse);
            if (!sendResponse.Success)
            {
                _context.Vtex.Logger.Warn("SaveAntifraudData", null, $"Failed",
                    new[]
                {
                        ( "StatusCode", sendResponse.StatusCode ),
                        ( "Message", sendResponse.Message ),
                        ( "id", id ),
                        ( "antifraudDataResponse", jsonSerializedAntifraudDataResponse )
                });
            }
        }

        public async Task<CreatePaymentRequest> GetCreatePaymentRequest(string id)
        {
            CreatePaymentRequest createPaymentRequest = null;
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Get,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT_REQUEST}/files/{id}",
                null);
            if (sendResponse.StatusCode == HttpStatusCode.NotFound.ToString())
            {
                _context.Vtex.Logger.Warn("GetAntifraudData", null, "Not Found.", new[] { ("id", id) });
                return null;
            }
            else
            {
                try
                {
                    createPaymentRequest = JsonConvert.DeserializeObject<CreatePaymentRequest>(sendResponse.Message);
                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("GetCreatePaymentRequest", null,
                    "Error:", ex,
                    new[]
                    {
                    ( "id", id )
                    });
                }
            }

            return createPaymentRequest;
        }

        public async Task SaveCreatePaymentRequest(string id, CreatePaymentRequest createPaymentRequest)
        {
            if (createPaymentRequest == null)
            {
                createPaymentRequest = new CreatePaymentRequest();
            }

            var jsonSerializedCreatePaymentRequest = JsonConvert.SerializeObject(createPaymentRequest);
            SendResponse sendResponse = await this.SendRequest(
                HttpMethod.Put,
                $"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT_REQUEST}/files/{id}",
                jsonSerializedCreatePaymentRequest);
            if (!sendResponse.Success)
            {
                _context.Vtex.Logger.Warn("SaveCreatePaymentRequest", null, $"Failed",
                    new[]
                {
                        ( "StatusCode", sendResponse.StatusCode ),
                        ( "Message", sendResponse.Message ),
                        ( "id", id ),
                        ( "createPaymentRequest", jsonSerializedCreatePaymentRequest )
                });
            }
        }

        public async Task<bool> CacheTaxResponse(VtexTaxResponse vtexTaxResponse, int cacheKey)
        {
            var jsonSerializedTaxResponse = JsonConvert.SerializeObject(vtexTaxResponse);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.CACHE_BUCKET}/files/{cacheKey}"),
                Content = new StringContent(jsonSerializedTaxResponse, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            bool IsSuccessStatusCode = false;

            try
            {
                var response = await client.SendAsync(request);
                IsSuccessStatusCode = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CacheTaxResponse", null, 
                "Error:", ex,
                new[]
                {
                    ( "vtexTaxResponse", jsonSerializedTaxResponse ),
                    ( "cacheKey", cacheKey.ToString() )
                });
            }

            return IsSuccessStatusCode;
        }

        public async Task<VtexTaxResponse> GetCachedTaxResponse(int cacheKey)
        {
            VtexTaxResponse vtexTaxResponse = null;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.CACHE_BUCKET}/files/{cacheKey}")
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
                    vtexTaxResponse = JsonConvert.DeserializeObject<VtexTaxResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetCachedTaxResponse", null, 
                "Error:", ex,
                new[]
                {
                    ( "cacheKey", cacheKey.ToString() )
                });
            }

            return vtexTaxResponse;
        }

        public bool TryGetCache(int cacheKey, out VtexTaxResponse vtexTaxResponse)
        {
            bool success = false;
            vtexTaxResponse = null;
            try
            {
                vtexTaxResponse = GetCachedTaxResponse(cacheKey).Result;
                success = vtexTaxResponse != null;
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("TryGetCache", null, 
                "Error getting cache:", ex,
                new[]
                {
                    ( "cacheKey", cacheKey.ToString() )
                });
            }

            return success;
        }

        public async Task<bool> SetCache(int cacheKey, VtexTaxResponse vtexTaxResponse)
        {
            bool success = false;

            try
            {
                success = await CacheTaxResponse(vtexTaxResponse, cacheKey);
                if (success)
                {
                    await _cachedKeys.AddCacheKey(cacheKey);
                }

                List<int> keysToRemove = await _cachedKeys.ListExpiredKeys();
                foreach (int cacheKeyToRemove in keysToRemove)
                {
                    await CacheTaxResponse(null, cacheKey);
                    await _cachedKeys.RemoveCacheKey(cacheKey);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetCache", null, 
                "Error setting cache:", ex,
                new[]
                {
                    ( "cacheKey", cacheKey.ToString() )
                });
            }

            return success;
        }

        public async Task<string> GetOrderConfiguration()
        {
            // https://{{accountName}}.vtexcommercestable.com.br/api/checkout/pvt/configuration/orderForm
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/checkout/pvt/configuration/orderForm"),
            };

            request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            string responseContent = string.Empty;

            try
            {
                var response = await client.SendAsync(request);
                responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                // A helper method is in order for this as it does not return the stack trace etc.
                response.EnsureSuccessStatusCode();

            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetOrderConfiguration", null, "Error:", ex);
            }
            
            return responseContent;
        }

        public async Task<bool> SetOrderConfiguration(string jsonSerializedOrderConfig)
        {
            // https://{{accountName}}.vtexcommercestable.com.br/api/checkout/pvt/configuration/orderForm
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/checkout/pvt/configuration/orderForm"),
                Content = new StringContent(jsonSerializedOrderConfig, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            bool IsSuccessStatusCode = false;

            try
            {
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                _context.Vtex.Logger.Info("SetOrderConfiguration", null,
                JsonConvert.SerializeObject(new[]
                {
                    ( "Request", jsonSerializedOrderConfig ),
                    ( "StatusCode", response.StatusCode.ToString() ),
                    ( "responseContent", responseContent )
                }));

                IsSuccessStatusCode = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SetOrderConfiguration", null, 
                "Error: ", ex,
                new[]
                {
                    ( "jsonSerializedOrderConfig", jsonSerializedOrderConfig )
                });
            }

            return IsSuccessStatusCode;
        }

        public async Task<SendResponse> SendRequest(HttpMethod method, string endpoint, string jsonSerializedData)
        {
            SendResponse sendResponse = null;
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = method,
                    RequestUri = new Uri(endpoint)
                };

                if (!string.IsNullOrEmpty(jsonSerializedData))
                {
                    request.Content = new StringContent(jsonSerializedData, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON);
                }

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
                sendResponse = new SendResponse
                {
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode,
                    Message = responseContent
                };
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SendRequest", null, $"Error ", ex, new[]
                {
                    ("method", method.ToString()),
                    ("endpoint", endpoint),
                    ("jsonSerializedData", jsonSerializedData)
                });
            }

            return sendResponse;
        }
    }
}
