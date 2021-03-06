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

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://apps.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/apps/{CybersourceConstants.APP_SETTINGS}/settings"),
                };

                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    merchantSettings = JsonConvert.DeserializeObject<MerchantSettings>(responseContent);
                }
            }
            catch(Exception ex)
            {
                _context.Vtex.Logger.Error("GetMerchantSettings", null, null, ex);
            }

            return merchantSettings;
        }

        public async Task<bool> SetMerchantSettings(MerchantSettings merchantSettings)
        {
            var jsonSerializedMerchantSettings = JsonConvert.SerializeObject(merchantSettings);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://apps.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/apps/{CybersourceConstants.APP_SETTINGS}/settings"),
                Content = new StringContent(jsonSerializedMerchantSettings, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            return response.IsSuccessStatusCode;
        }

        public async Task<PaymentData> GetPaymentData(string paymentIdentifier)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT}/files/{paymentIdentifier}"),
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _context.Vtex.Logger.Warn("GetPaymentData", null, $"Payment Id '{paymentIdentifier}' Not Found.");
                return null;
            }

            PaymentData paymentData =  JsonConvert.DeserializeObject<PaymentData>(responseContent);

            return paymentData;
        }

        public async Task SavePaymentData(string paymentIdentifier, PaymentData paymentData)
        {
            if (paymentData == null)
            {
                paymentData = new PaymentData();
            }

            var jsonSerializedCreatePaymentRequest = JsonConvert.SerializeObject(paymentData);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_PAYMENT}/files/{paymentIdentifier}"),
                Content = new StringContent(jsonSerializedCreatePaymentRequest, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }

        public async Task<SendAntifraudDataResponse> GetAntifraudData(string id)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_ANTIFRAUD}/files/{id}"),
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _context.Vtex.Logger.Warn("GetAntifraudData", null, $"Id '{id}' Not Found.");
                return null;
            }

            SendAntifraudDataResponse antifraudDataResponse =  JsonConvert.DeserializeObject<SendAntifraudDataResponse>(responseContent);
            _context.Vtex.Logger.Debug("GetAntifraudData", null, id, new[] { ("responseContent", responseContent) });

            return antifraudDataResponse;
        }

        public async Task SaveAntifraudData(string id, SendAntifraudDataResponse antifraudDataResponse)
        {
            if (antifraudDataResponse == null)
            {
                antifraudDataResponse = new SendAntifraudDataResponse();
            }

            var jsonSerializedCreatePaymentRequest = JsonConvert.SerializeObject(antifraudDataResponse);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://vbase.{this._environmentVariableProvider.Region}.vtex.io/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_ANTIFRAUD}/files/{id}"),
                Content = new StringContent(jsonSerializedCreatePaymentRequest, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _context.Vtex.Logger.Error("SaveAntifraudData", null, "Did not save Antifraud Data.", null, new[] { ("Id", id), ("AntifraudDataResponse", jsonSerializedCreatePaymentRequest) });
            }
            else
            {
                _context.Vtex.Logger.Debug("SaveAntifraudData", null, "Saved Antifraud Data.", new[] { ("Id", id), ("AntifraudDataResponse", jsonSerializedCreatePaymentRequest) });
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
            var response = await client.SendAsync(request);

            return response.IsSuccessStatusCode;
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
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                vtexTaxResponse = JsonConvert.DeserializeObject<VtexTaxResponse>(responseContent);
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
                _context.Vtex.Logger.Error("TryGetCache", null, "Error getting cache", ex);
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
                _context.Vtex.Logger.Error("TryGetCache", null, "Error setting cache", ex);
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
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // A helper method is in order for this as it does not return the stack trace etc.
            response.EnsureSuccessStatusCode();

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
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            _context.Vtex.Logger.Info("SetOrderConfiguration", null, $"Request:\r{jsonSerializedOrderConfig}\rResponse: [{response.StatusCode}]\r{responseContent}");

            return response.IsSuccessStatusCode;
        }
    }
}
