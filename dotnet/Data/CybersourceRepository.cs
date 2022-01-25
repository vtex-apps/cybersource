namespace Cybersource.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Cybersource.Models;
    using Cybersource.Services;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Vtex.Api.Context;
    using Cybersource.Data;

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
            Console.WriteLine($" - GetMerchantSettings - '{responseContent}'");
            if (response.IsSuccessStatusCode)
            {
                merchantSettings = JsonConvert.DeserializeObject<MerchantSettings>(responseContent);
                if(merchantSettings.MerchantDefinedValues == null)
                {
                    merchantSettings.MerchantDefinedValues = new Dictionary<string, string>();
                    merchantSettings.MerchantDefinedValues.Add("MID", "{{MerchantId}}");
                    merchantSettings.MerchantDefinedValues.Add("Order Type", "{{CompanyName}}");
                    merchantSettings.MerchantDefinedValues.Add("CALL CENTER", "{{CompanyTaxId}}");
                    merchantSettings.MerchantDefinedValues.Add("Customer Name", "{{CustomerName}}");
                    merchantSettings.MerchantDefinedValues.Add("Total Cart Amount", "{{TotalCartValue}}");
                    //merchantSettings.MerchantDefinedValues.Add("Concat Test", "TEST:{{OrderId}}-{{Reference}}");
                    //merchantSettings.MerchantDefinedValues.Add("PaymentId", "26940{{Reference|Pad|9:0}}{{|date|yy}}");
                    await this.SetMerchantSettings(merchantSettings);
                }
            }

            return merchantSettings;
        }

        public async Task SetMerchantSettings(MerchantSettings merchantSettings)
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
                return null;
            }

            SendAntifraudDataResponse antifraudDataResponse =  JsonConvert.DeserializeObject<SendAntifraudDataResponse>(responseContent);

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

            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> CacheTaxResponse(VtexTaxResponse vtexTaxResponse, int cacheKey)
        {
            var jsonSerializedProducReviews = JsonConvert.SerializeObject(vtexTaxResponse);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}/buckets/{this._applicationName}/{CybersourceConstants.CACHE_BUCKET}/files/{cacheKey}"),
                Content = new StringContent(jsonSerializedProducReviews, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
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

        public async Task<CybersourceToken> LoadToken(bool isProduction)
        {
            string filename = isProduction ? CybersourceConstants.TOKEN_LIVE : CybersourceConstants.TOKEN_TEST;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_TOKEN}/files/{filename}")
            };

            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            request.Headers.Add("Cache-Control", "no-cache");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _context.Vtex.Logger.Info("LoadToken", null, "Token not found!");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            _context.Vtex.Logger.Info("LoadToken", null, responseContent);
            CybersourceToken token = JsonConvert.DeserializeObject<CybersourceToken>(responseContent);

            return token;
        }

        public async Task<bool> SaveToken(CybersourceToken token, bool isProduction)
        {
            string filename = isProduction ? CybersourceConstants.TOKEN_LIVE : CybersourceConstants.TOKEN_TEST;
            var jsonSerializedToken = JsonConvert.SerializeObject(token);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"http://infra.io.vtex.com/vbase/v2/{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}/{this._environmentVariableProvider.Workspace}/buckets/{this._applicationName}/{CybersourceConstants.BUCKET_TOKEN}/files/{filename}"),
                Content = new StringContent(jsonSerializedToken, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
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
            _context.Vtex.Logger.Info("SaveToken", null, $"[{response.StatusCode}] '{responseContent}' {jsonSerializedToken}");
            return response.IsSuccessStatusCode;
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
            //string authToken = _context.Vtex.AdminUserAuthToken;
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"GetOrderConfiguration [{response.StatusCode}] ");
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
                RequestUri = new Uri($"https://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/checkout/pvt/configuration/orderForm"),
                Content = new StringContent(jsonSerializedOrderConfig, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
            };

            request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
            string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
            //string authToken = _context.Vtex.AdminUserAuthToken;
            Console.WriteLine($"    authToken   = '{authToken}'    ");
            if (authToken != null)
            {
                request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
            }

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"SetOrderConfiguration [{response.StatusCode}] ");
            _context.Vtex.Logger.Info("SetOrderConfiguration", null, $"Request:\r{jsonSerializedOrderConfig}\rResponse: [{response.StatusCode}]\r{responseContent}");

            return response.IsSuccessStatusCode;
        }
    }
}
