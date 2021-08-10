using Cybersource.Data;
using Cybersource.Models;
using Cybersource.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Vtex.Api.Context;

namespace Cybersource.Services
{
    public class VtexApiService : IVtexApiService
    {
        private readonly IIOServiceContext _context;
        private readonly IVtexEnvironmentVariableProvider _environmentVariableProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ICybersourceRepository _cybersourceRepository;
        private readonly string _applicationName;

        public VtexApiService(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ICybersourceRepository cybersourceRepository)
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

        public async Task<VtexOrder> GetOrderInformation(string orderId)
        {
            VtexOrder vtexOrder = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders/{orderId}")
                };

                request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    request.Headers.Add(CybersourceConstants.VTEX_ID_HEADER_NAME, authToken);
                    request.Headers.Add(CybersourceConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                //StringBuilder sb = new StringBuilder();

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    vtexOrder = JsonConvert.DeserializeObject<VtexOrder>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Info("GetOrderInformation", null, $"Order# {orderId} [{response.StatusCode}] '{responseContent}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetOrderInformation", null, $"Order# {orderId} Error", ex);
            }

            return vtexOrder;
        }

        public async Task<VtexDockResponse[]> ListVtexDocks()
        {
            VtexDockResponse[] listVtexDocks = null;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/logistics/pvt/configuration/docks")
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
                listVtexDocks = JsonConvert.DeserializeObject<VtexDockResponse[]>(responseContent);
            }

            return listVtexDocks;
        }

        public async Task<VtexDockResponse> ListDockById(string dockId)
        {
            VtexDockResponse dockResponse = null;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/logistics/pvt/configuration/docks/{dockId}")
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
                dockResponse = JsonConvert.DeserializeObject<VtexDockResponse>(responseContent);
            }

            return dockResponse;
        }

        public async Task<PickupPoints> ListPickupPoints()
        {
            PickupPoints pickupPoints = null;
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://logistics.vtexcommercestable.com.br/api/logistics/pvt/configuration/pickuppoints/_search?an={this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}&pageSize=100")
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
                pickupPoints = JsonConvert.DeserializeObject<PickupPoints>(responseContent);
            }

            return pickupPoints;
        }

        public async Task<GetSkuContextResponse> GetSku(string skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/skuId

            GetSkuContextResponse getSkuResponse = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/{skuId}")
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
                    getSkuResponse = JsonConvert.DeserializeObject<GetSkuContextResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetSku", null, $"Did not get context for skuid '{skuId}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetSku", null, $"Error getting context for skuid '{skuId}'", ex);
            }

            return getSkuResponse;
        }

        public async Task<string> InitConfiguration()
        {
            string retval = string.Empty;
            //MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //string jsonSerializedOrderConfig = await this._cybersourceRepository.GetOrderConfiguration();
            //if (string.IsNullOrEmpty(jsonSerializedOrderConfig))
            //{
            //    retval = "Could not load Order Configuration.";
            //}
            //else
            //{
            //    dynamic orderConfig = JsonConvert.DeserializeObject(jsonSerializedOrderConfig);
            //    VtexOrderformTaxConfiguration taxConfiguration = new VtexOrderformTaxConfiguration
            //    {
            //        AllowExecutionAfterErrors = false,
            //        IntegratedAuthentication = true,
            //        Url = $"https://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}--{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.myvtex.com/taxjar/checkout/order-tax"
            //    };

            //    orderConfig["taxConfiguration"] = JToken.FromObject(taxConfiguration);

            //    jsonSerializedOrderConfig = JsonConvert.SerializeObject(orderConfig);
            //    bool success = await this._cybersourceRepository.SetOrderConfiguration(jsonSerializedOrderConfig);
            //    retval = success.ToString();
            //}

            return retval;
        }

        public async Task<string> RemoveConfiguration()
        {
            string retval = string.Empty;
            //MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //string jsonSerializedOrderConfig = await this._cybersourceRepository.GetOrderConfiguration();
            //if (string.IsNullOrEmpty(jsonSerializedOrderConfig))
            //{
            //    retval = "Could not load Order Configuration.";
            //}
            //else
            //{
            //    dynamic orderConfig = JsonConvert.DeserializeObject(jsonSerializedOrderConfig);
            //    VtexOrderformTaxConfiguration taxConfiguration = new VtexOrderformTaxConfiguration
            //    {

            //    };

            //    orderConfig["taxConfiguration"] = JToken.FromObject(taxConfiguration);

            //    jsonSerializedOrderConfig = JsonConvert.SerializeObject(orderConfig);
            //    bool success = await this._cybersourceRepository.SetOrderConfiguration(jsonSerializedOrderConfig);
            //    retval = success.ToString();
            //}

            return retval;
        }

        public async Task<TaxFallbackResponse> GetFallbackRate(string country, string postalCode, string provider = "avalara")
        {
            // GET https://vtexus.myvtex.com/_v/tax-fallback/{country}/{provider}/{postalCode}

            TaxFallbackResponse fallbackResponse = null;

            try
            {
                if (country.Length > 2)
                    country = country.Substring(0, 2);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://vtexus.myvtex.com/_v/tax-fallback/{country}/{provider}/{postalCode}")
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
                    fallbackResponse = JsonConvert.DeserializeObject<TaxFallbackResponse>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetFallbackRate", null, $"Did not get rates for {country} {postalCode} ({provider}) : '{response.Content}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetFallbackRate", null, $"Error getting rates for {country} {postalCode} ({provider})", ex);
            }

            return fallbackResponse;
        }

        public async Task<bool> ProcessNotification(AllStatesNotification allStatesNotification)
        {
            bool success = true;
            VtexOrder vtexOrder = null;

            switch (allStatesNotification.Domain)
            {
                case CybersourceConstants.Domain.Fulfillment:
                    switch (allStatesNotification.CurrentState)
                    {
                        case CybersourceConstants.VtexOrderStatus.Invoiced:
                            success = await this.ProcessInvoice(allStatesNotification.OrderId);
                            break;
                            break;
                        default:
                            //_context.Vtex.Logger.Info("ProcessNotification", null, $"State {hookNotification.State} not implemeted.");
                            break;
                    }
                    break;
                case CybersourceConstants.Domain.Marketplace:
                    switch (allStatesNotification.CurrentState)
                    {
                        default:
                            //_context.Vtex.Logger.Info("ProcessNotification", null, $"State {hookNotification.State} not implemeted.");
                            break;
                    }
                    break;
                default:
                    //_context.Vtex.Logger.Info("ProcessNotification", null, $"Domain {hookNotification.Domain} not implemeted.");
                    break;
            }

            return success;
        }

        public async Task<bool> ProcessInvoice(string orderId)
        {
            bool success = true;
            //MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            //if (merchantSettings.EnableTransactionPosting)
            //{
            //    VtexOrder vtexOrder = await this.GetOrderInformation(orderId);
            //    if (vtexOrder != null)
            //    {
            //        if (!string.IsNullOrEmpty(merchantSettings.SalesChannelExclude))
            //        {
            //            string[] salesChannelsToExclude = merchantSettings.SalesChannelExclude.Split(',');
            //            if (salesChannelsToExclude.Contains(vtexOrder.SalesChannel))
            //            {
            //                _context.Vtex.Logger.Debug("ProcessInvoiceHook", null, $"Order '{orderId}' skipping sales channel '{vtexOrder.SalesChannel}'");
            //                return success;
            //            }
            //        }

            //        CreateTaxjarOrder taxjarOrder = await this.VtexOrderToTaxjarOrder(vtexOrder);
            //        _context.Vtex.Logger.Debug("CreateTaxjarOrder", null, $"{JsonConvert.SerializeObject(taxjarOrder)}");
            //        OrderResponse orderResponse = await _taxjarService.CreateOrder(taxjarOrder);
            //        if (orderResponse != null)
            //        {
            //            _context.Vtex.Logger.Debug("ProcessInvoiceHook", null, $"Order '{orderId}' taxes were committed");
            //        }
            //        else
            //        {
            //            success = false;
            //        }
            //    }
            //}

            return success;
        }
    }
}
