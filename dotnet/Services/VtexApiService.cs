using Cybersource.Data;
using Cybersource.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ICybersourceApi _cybersourceApi;
        private readonly string _applicationName;

        public VtexApiService(IIOServiceContext context, IVtexEnvironmentVariableProvider environmentVariableProvider, IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory, ICybersourceRepository cybersourceRepository, ICybersourceApi cybersourceApi)
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

            this._cybersourceApi = cybersourceApi ??
                               throw new ArgumentNullException(nameof(cybersourceApi));

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

        public async Task<VtexOrder[]> GetOrderGroup(string orderId)
        {
            VtexOrder[] vtexOrders = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/checkout/pub/orders/order-group/{orderId}")
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
                    vtexOrders = JsonConvert.DeserializeObject<VtexOrder[]>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Info("GetOrderGroup", null, $"Order# {orderId} [{response.StatusCode}] '{responseContent}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetOrderGroup", null, $"Order# {orderId} Error", ex);
            }

            return vtexOrders;
        }

        public async Task<VtexOrder[]> LookupOrders(string orderId)
        {
            VtexOrder[] vtexOrders = null;
            try
            {
                VtexOrderList vtexOrderList = await this.SearchOrders(orderId);
                string lookupOrderId = vtexOrderList.List.First().OrderId;
                int charLocation = lookupOrderId.IndexOf("-", StringComparison.Ordinal);
                if (charLocation > 0)
                {
                    lookupOrderId = lookupOrderId.Substring(0, charLocation);
                }

                vtexOrders = await this.GetOrderGroup(lookupOrderId);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("LookupOrders", null, $"Order# {orderId} Error", ex);
            }

            return vtexOrders;
        }

        public async Task<string> GetSequence(string orderId)
        {
            string sequence = orderId; // default to original value
            try
            {
                VtexOrderList vtexOrderList = await this.SearchOrders(orderId);
                sequence = vtexOrderList.List.First().Sequence;
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("LookupOrders", null, $"Order# {orderId} Error", ex);
            }

            return sequence;
        }

        public async Task<string> GetOrderId(string reference)
        {
            string orderId = reference; // default to original value
            try
            {
                VtexOrderList vtexOrderList = await this.SearchOrders(orderId);
                orderId = vtexOrderList.List.First().OrderId;
                int charLocation = orderId.IndexOf("-", StringComparison.Ordinal);
                if (charLocation > 0)
                {
                    orderId = orderId.Substring(0, charLocation);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetOrderId", null, $"Reference# {reference} Error", ex);
            }

            return orderId;
        }

        public async Task<VtexOrderList> SearchOrders(string query)
        {
            VtexOrderList vtexOrderList = null;
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders?q={query}")
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
                    vtexOrderList = JsonConvert.DeserializeObject<VtexOrderList>(responseContent);
                }
                else
                {
                    _context.Vtex.Logger.Info("SearchOrders", null, $"Query:{query} [{response.StatusCode}] '{responseContent}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("SearchOrders", null, $"Query:{query} Error", ex);
            }

            return vtexOrderList;
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
            string jsonSerializedOrderConfig = await this._cybersourceRepository.GetOrderConfiguration();
            if (string.IsNullOrEmpty(jsonSerializedOrderConfig))
            {
                retval = "Could not load Order Configuration.";
            }
            else
            {
                dynamic orderConfig = JsonConvert.DeserializeObject(jsonSerializedOrderConfig);
                VtexOrderformTaxConfiguration taxConfiguration = new VtexOrderformTaxConfiguration
                {
                    AllowExecutionAfterErrors = false,
                    IntegratedAuthentication = true,
                    Url = $"https://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_WORKSPACE]}--{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.myvtex.com/cybersource/checkout/order-tax"
                };

                orderConfig["taxConfiguration"] = JToken.FromObject(taxConfiguration);

                jsonSerializedOrderConfig = JsonConvert.SerializeObject(orderConfig);
                bool success = await this._cybersourceRepository.SetOrderConfiguration(jsonSerializedOrderConfig);
                retval = success.ToString();
            }

            return retval;
        }

        public async Task<string> RemoveConfiguration()
        {
            string retval = string.Empty;
            string jsonSerializedOrderConfig = await this._cybersourceRepository.GetOrderConfiguration();
            if (string.IsNullOrEmpty(jsonSerializedOrderConfig))
            {
                retval = "Could not load Order Configuration.";
            }
            else
            {
                VtexOrderformTaxConfiguration taxConfiguration = null;
                dynamic orderConfig = JsonConvert.DeserializeObject(jsonSerializedOrderConfig);
                try
                {
                    string existingTaxConfig = JsonConvert.SerializeObject(orderConfig["taxConfiguration"]);
                    if (!string.IsNullOrEmpty(existingTaxConfig))
                    {
                        taxConfiguration = JsonConvert.DeserializeObject<VtexOrderformTaxConfiguration>(existingTaxConfig);
                    }
                }
                catch(Exception ex)
                {
                    _context.Vtex.Logger.Error("RemoveConfiguration", null, "Error getting existing config", ex);
                }

                if (taxConfiguration != null && taxConfiguration.Url.Contains("cybersource"))
                {
                    taxConfiguration = new VtexOrderformTaxConfiguration
                    {

                    };

                    orderConfig["taxConfiguration"] = JToken.FromObject(taxConfiguration);

                    jsonSerializedOrderConfig = JsonConvert.SerializeObject(orderConfig);
                    bool success = await this._cybersourceRepository.SetOrderConfiguration(jsonSerializedOrderConfig);
                    retval = success.ToString();
                }
                else
                {
                    retval = $"Not configured for Cybersouce";
                }
            }

            return retval;
        }

        public async Task<TaxFallbackResponse> GetFallbackRate(string country, string postalCode, string provider = "avalara")
        {
            // GET https://vtexus.myvtex.com/_v/tax-fallback/{country}/{provider}/{postalCode}

            TaxFallbackResponse fallbackResponse = null;

            try
            {
                if (country.Length > 2)
                    country = this.GetCountryCode(country);

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

        public async Task<VtexTaxResponse> GetFallbackTaxes(VtexTaxRequest taxRequest)
        {
            VtexTaxResponse vtexTaxResponse = null;
            TaxFallbackResponse fallbackResponse = await this.GetFallbackRate(taxRequest.ShippingDestination.Country, taxRequest.ShippingDestination.PostalCode);
            if (fallbackResponse != null)
            {
                vtexTaxResponse = new VtexTaxResponse
                {
                    Hooks = new Hook[] { },
                    ItemTaxResponse = new List<ItemTaxResponse>()
                };

                long totalQuantity = taxRequest.Items.Sum(i => i.Quantity);
                for (int i = 0; i < taxRequest.Items.Length; i++)
                {
                    Item item = taxRequest.Items[i];
                    double itemTaxPercentOfWhole = (double)item.Quantity / totalQuantity;
                    ItemTaxResponse itemTaxResponse = new ItemTaxResponse
                    {
                        Id = item.Id
                    };

                    List<VtexTax> vtexTaxes = new List<VtexTax>();
                    if (fallbackResponse.StateSalesTax > 0)
                    {
                        vtexTaxes.Add(
                            new VtexTax
                            {
                                Description = "",
                                Name = $"STATE TAX: {fallbackResponse.StateAbbrev}",
                                Value = item.ItemPrice * fallbackResponse.StateSalesTax
                            }
                         );
                    }

                    if (fallbackResponse.CountySalesTax > 0)
                    {
                        vtexTaxes.Add(
                        new VtexTax
                        {
                            Description = "",
                            Name = $"COUNTY TAX: {fallbackResponse.CountyName}",
                            Value = item.ItemPrice * fallbackResponse.CountySalesTax
                        }
                     );
                    }

                    if (fallbackResponse.CitySalesTax > 0)
                    {
                        vtexTaxes.Add(
                        new VtexTax
                        {
                            Description = "",
                            Name = $"CITY TAX: {fallbackResponse.CityName}",
                            Value = item.ItemPrice * fallbackResponse.CitySalesTax
                        }
                     );
                    }

                    if (fallbackResponse.TaxShippingAlone || fallbackResponse.TaxShippingAndHandlingTogether)
                    {
                        decimal shippingTotal = taxRequest.Totals.Where(t => t.Id.Contains("Shipping")).Sum(t => t.Value) / 100;
                        vtexTaxes.Add(
                        new VtexTax
                        {
                            Description = "",
                            Name = $"TAX: (SHIPPING)",
                            Value = (decimal)Math.Round((double)shippingTotal * (double)fallbackResponse.TotalSalesTax * itemTaxPercentOfWhole, 2, MidpointRounding.ToEven)
                        }
                      );
                    }

                    itemTaxResponse.Taxes = vtexTaxes.ToArray();
                    vtexTaxResponse.ItemTaxResponse.Add(itemTaxResponse);
                }
            }

            return vtexTaxResponse;
        }

        public async Task<VtexTaxResponse> GetTaxes(VtexTaxRequest taxRequest, VtexTaxRequest taxRequestOriginal)
        {
            //_context.Vtex.Logger.Debug("GetTaxes", null, $"VtexTaxRequest\n{JsonConvert.SerializeObject(taxRequest)}");

            // Combine skus
            //Dictionary<string, Item> itemDictionary = new Dictionary<string, Item>();
            //foreach (Item requestItem in taxRequest.Items)
            //{
            //    if (itemDictionary.ContainsKey(requestItem.Sku))
            //    {
            //        itemDictionary[requestItem.Sku].DiscountPrice += requestItem.DiscountPrice;
            //        itemDictionary[requestItem.Sku].ItemPrice += requestItem.ItemPrice;
            //        itemDictionary[requestItem.Sku].Quantity += requestItem.Quantity;
            //    }
            //    else
            //    {
            //        itemDictionary.Add(requestItem.Sku, requestItem);
            //    }
            //}

            //taxRequest.Items = itemDictionary.Values.ToArray();

            VtexTaxResponse vtexTaxResponse = new VtexTaxResponse
            {
                ItemTaxResponse = new List<ItemTaxResponse>()
            };

            bool inNexus = await this.InNexus(taxRequest.ShippingDestination.State, taxRequest.ShippingDestination.Country);
            if (inNexus)
            {
                Payments cyberTaxRequest = new Payments
                {
                    clientReferenceInformation = new ClientReferenceInformation
                    {
                        code = taxRequest.OrderFormId
                    },
                    taxInformation = new TaxInformation
                    {
                        //nexus = new List<string>(),
                        showTaxPerLineItem = "yes"
                    },
                    orderInformation = new OrderInformation
                    {
                        //amountDetails = new AmountDetails
                        //{
                        //    currency = 
                        //},
                        billTo = new BillTo
                        {
                            address1 = taxRequest.ShippingDestination.Street,
                            administrativeArea = taxRequest.ShippingDestination.State,
                            country = this.GetCountryCode(taxRequest.ShippingDestination.Country),
                            postalCode = taxRequest.ShippingDestination.PostalCode,
                            locality = taxRequest.ShippingDestination.City
                        },
                        shipTo = new ShipTo
                        {
                            address1 = taxRequest.ShippingDestination.Street,
                            administrativeArea = taxRequest.ShippingDestination.State,
                            country = this.GetCountryCode(taxRequest.ShippingDestination.Country),
                            postalCode = taxRequest.ShippingDestination.PostalCode,
                            locality = taxRequest.ShippingDestination.City
                        },
                        lineItems = new List<LineItem>()
                    },
                };

                VtexDockResponse[] vtexDocks = await this.ListVtexDocks();

                foreach (Item item in taxRequest.Items)
                {
                    //_context.Vtex.Logger.Debug("GetTaxes", null, $"Sku:{item.Sku} #{item.Quantity} x{item.UnitMultiplier}\n{item.ItemPrice} -{item.DiscountPrice} ={item.TargetPrice}");
                    string dockId = item.DockId;
                    VtexDockResponse vtexDock = vtexDocks.FirstOrDefault(d => d.Id.Equals(dockId));

                    string taxCode = "default";
                    string productName = string.Empty;
                    GetSkuContextResponse skuContextResponse = await this.GetSku(item.Sku);
                    if (skuContextResponse != null)
                    {
                        taxCode = skuContextResponse.TaxCode;
                        productName = skuContextResponse.SkuName;
                    }

                    LineItem lineItem = new LineItem
                    {
                        productSKU = item.Sku,
                        productCode = taxCode, //"default",
                        quantity = item.Quantity.ToString(),
                        productName = productName,
                        unitPrice = (item.TargetPrice + (item.DiscountPrice / item.Quantity)).ToString("0.00"), // DiscountPrice is negative
                                                                                                                //commodityCode = taxCode,
                        shipFromCountry = this.GetCountryCode(vtexDock?.PickupStoreInfo?.Address?.Country?.Acronym),
                        shipFromAdministrativeArea = vtexDock?.PickupStoreInfo?.Address?.State,
                        shipFromLocality = vtexDock?.PickupStoreInfo?.Address?.City,
                        shipFromPostalCode = vtexDock?.PickupStoreInfo?.Address?.PostalCode
                    };

                    cyberTaxRequest.orderInformation.lineItems.Add(lineItem);
                }

                // Add shipping as an item
                decimal shippingAmount = taxRequest.Totals.Where(t => t.Id.Contains("Shipping")).Sum(t => t.Value) / 100;
                LineItem lineItemShipping = new LineItem
                {
                    productName = "Shipping",
                    productCode = "shipping",
                    productSKU = "Shipping",
                    unitPrice = shippingAmount.ToString("0.00"),
                    quantity = "1"
                };

                cyberTaxRequest.orderInformation.lineItems.Add(lineItemShipping);

                PaymentsResponse taxResponse = await _cybersourceApi.CalculateTaxes(cyberTaxRequest);
                if (taxResponse != null)
                {
                    if (taxResponse.Status.Equals("COMPLETED"))
                    {
                        vtexTaxResponse = await this.CybersourceResponseToVtexResponse(taxResponse);
                        string splitSkus = taxRequest.Items.Length.Equals(taxRequestOriginal.Items.Length) ? "Skus were not combined" : $"Skus combined. {taxRequestOriginal.Items.Length} line items combined to {taxRequest.Items.Length}";
                        _context.Vtex.Logger.Debug("GetTaxes", "CalculateTaxes", splitSkus, new[] { ("cyberTaxRequest", JsonConvert.SerializeObject(cyberTaxRequest)), ("taxResponse", JsonConvert.SerializeObject(taxResponse)), ("vtexTaxResponse", JsonConvert.SerializeObject(vtexTaxResponse)) });
                    }
                    else
                    {
                        _context.Vtex.Logger.Error("GetTaxes", "CalculateTaxes", $"Tax Response Status = '{taxResponse.Status}'", null, new[] { ("cyberTaxRequest", JsonConvert.SerializeObject(cyberTaxRequest)), ("taxResponse", JsonConvert.SerializeObject(taxResponse)) });
                        return null;
                    }
                }
                else
                {
                    _context.Vtex.Logger.Error("GetTaxes", "CalculateTaxes", "Tax Response is Null.", null, new[] { ("cyberTaxRequest", JsonConvert.SerializeObject(cyberTaxRequest)) });
                    return null;
                }

                _context.Vtex.Logger.Debug("vtexTaxResponse", null, JsonConvert.SerializeObject(vtexTaxResponse));

                // Split response items to match request
                try
                {
                    // Split out skus to match request
                    if (taxRequest.Items.Length != taxRequestOriginal.Items.Length)
                    {
                        Console.WriteLine($"taxRequest.Items.Length {taxRequest.Items.Length} != taxRequestOriginal.Items.Length {taxRequestOriginal.Items.Length}");
                        Console.WriteLine($"vtexTaxResponse.ItemTaxResponse.Count = {vtexTaxResponse.ItemTaxResponse.Count}");
                        ItemTaxResponse[] itemTaxResponses = new ItemTaxResponse[taxRequestOriginal.Items.Length];
                        int responseId = 0;
                        decimal totalSplitAllocatedTax = 0m;
                        Dictionary<string, long> qntyAllocatedPerSku = new Dictionary<string, long>();
                        foreach (Item requestItem in taxRequestOriginal.Items)
                        {
                            Item trItem = taxRequest.Items.FirstOrDefault(i => i.Sku.Equals(requestItem.Sku));
                            Console.WriteLine($"trItem: {trItem.Id} sku [{requestItem.Sku}] id: {requestItem.Id}");
                            string taxResponseIndexId = trItem.Id;
                            if (requestItem.Quantity == trItem.Quantity)
                            {
                                itemTaxResponses[responseId] = vtexTaxResponse.ItemTaxResponse.FirstOrDefault(t => t.Id.Equals(taxResponseIndexId));
                                if(itemTaxResponses[responseId] == null)
                                {
                                    Console.WriteLine($"responseId {responseId} == NULL!!! taxResponseIndexId [{taxResponseIndexId}]");
                                    return null;
                                }

                                //Console.WriteLine($"ADDING {responseId} == ");
                            }
                            else
                            {
                                decimal percentOfTotal = 0m;
                                if (trItem.ItemPrice + trItem.DiscountPrice > 0)
                                {
                                    percentOfTotal = (requestItem.ItemPrice + requestItem.DiscountPrice) / (trItem.ItemPrice + trItem.DiscountPrice);
                                }

                                ItemTaxResponse itemTaxResponse = vtexTaxResponse.ItemTaxResponse.FirstOrDefault(t => t.Id.Equals(taxResponseIndexId));
                                ItemTaxResponse itemTaxResponseSplit = new ItemTaxResponse
                                {
                                    Id = responseId.ToString(),
                                    Taxes = new VtexTax[itemTaxResponse.Taxes.Length]
                                };

                                long taxObjIndex = 0;
                                foreach (VtexTax taxObj in itemTaxResponse.Taxes)
                                {
                                    itemTaxResponseSplit.Taxes[taxObjIndex] = new VtexTax
                                    {
                                        Description = taxObj.Description,
                                        Name = taxObj.Name,
                                        Value = Math.Round(taxObj.Value * percentOfTotal, 2, MidpointRounding.ToEven)
                                    };

                                    taxObjIndex++;
                                }

                                itemTaxResponses[responseId] = itemTaxResponseSplit;
                                Console.WriteLine($"ADDING {responseId} != ");
                                totalSplitAllocatedTax += itemTaxResponseSplit.Taxes.Sum(i => i.Value);

                                if (qntyAllocatedPerSku.ContainsKey(requestItem.Sku))
                                {
                                    qntyAllocatedPerSku[requestItem.Sku] += requestItem.Quantity;
                                }
                                else
                                {
                                    qntyAllocatedPerSku.Add(requestItem.Sku, requestItem.Quantity);
                                }

                                if (qntyAllocatedPerSku[requestItem.Sku] == trItem.Quantity)
                                {
                                    decimal totalTaxToAllocate = itemTaxResponse.Taxes.Sum(i => i.Value);
                                    if (totalSplitAllocatedTax != totalTaxToAllocate)
                                    {
                                        decimal adjustmentAmount = Math.Round((totalTaxToAllocate - totalSplitAllocatedTax), 2, MidpointRounding.ToEven);
                                        itemTaxResponses[responseId].Taxes.First().Value += adjustmentAmount;
                                        _context.Vtex.Logger.Warn("GetTaxes", null, $"Applying adjustment to id [{taxResponseIndexId}]: {totalTaxToAllocate} - {totalSplitAllocatedTax} = {adjustmentAmount}");
                                    }

                                    totalSplitAllocatedTax = 0m;
                                }
                            }

                            responseId++;
                        }

                        vtexTaxResponse.ItemTaxResponse = itemTaxResponses.ToList();
                        //foreach(ItemTaxResponse taxResp in itemTaxResponses)
                        //{
                        //    if(taxResp == null)
                        //    {
                        //        Console.WriteLine($" taxResp IS NULL!!!");
                        //    }

                        //    Console.WriteLine($"Adding taxResp {taxResp.Id}");
                        //    vtexTaxResponse.ItemTaxResponse.Add(taxResp);
                        //    Console.WriteLine($"Added taxResp {taxResp.Id}");
                        //}
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Error splitting line items: '{ex.Message}'");
                    _context.Vtex.Logger.Error("TaxjarResponseToVtexResponse", "Splitting", $"Error splitting line items", ex);
                    return null;
                }
            }
            else
            {
                _context.Vtex.Logger.Info("TaxHandler", null, $"Order '{taxRequest.OrderFormId}' Destination state '{taxRequest.ShippingDestination.State}' is NOT in nexus");
            }

            return vtexTaxResponse;
        }

        public async Task<bool> InNexus(string state, string country)
        {
            bool inNexus = false;
            bool usedMerchantSettings = false;
            if (country.Length > 2)
            {
                country = this.GetCountryCode(country);
            }

            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            if(merchantSettings != null && !string.IsNullOrWhiteSpace(merchantSettings.NexusRegions))
            {
                usedMerchantSettings = true;
                string[] arrCountry = merchantSettings.NexusRegions.Split(':');
                foreach(string stateList in arrCountry)
                {
                    string[] countryWithStates = stateList.Split('=');
                    string tmpCountry = countryWithStates[0];
                    if(tmpCountry.Equals(country))
                    {
                        string[] stateArr = countryWithStates[1].Split(',');
                        if(stateArr.Contains(state))
                        {
                            inNexus = true;
                            break;
                        }
                    }
                }
            }

            if (!usedMerchantSettings)
            {
                PickupPoints pickupPoints = await this.ListPickupPoints();
                if (pickupPoints != null)
                {
                    //List<string> nexusStates = new List<string>();
                    foreach (PickupPointItem pickupPoint in pickupPoints.Items)
                    {
                        if (pickupPoint != null &&
                            pickupPoint.Address != null &&
                            pickupPoint.Address.State != null &&
                            pickupPoint.Address.Country != null &&
                            pickupPoint.Address.State.Equals(state) &&
                            this.GetCountryCode(pickupPoint.Address.Country.Acronym).Equals(country))
                        {
                            inNexus = true;
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"{pickupPoint.Name} not InNexus");
                        }
                    }
                }
            }

            return inNexus;
        }

        public async Task<bool> ProcessNotification(AllStatesNotification allStatesNotification)
        {
            bool success = true;

            switch (allStatesNotification.Domain)
            {
                case CybersourceConstants.Domain.Fulfillment:
                    switch (allStatesNotification.CurrentState)
                    {
                        case CybersourceConstants.VtexOrderStatus.Invoiced:
                            success = await this.ProcessInvoice(allStatesNotification.OrderId);
                            break;
                    }

                    break;
            }

            return success;
        }

        public async Task<bool> ProcessInvoice(string orderId)
        {
            bool success = true;
            MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
            if (merchantSettings.EnableTransactionPosting)
            {
                VtexOrder vtexOrder = await this.GetOrderInformation(orderId);
                if (vtexOrder != null)
                {
                    if (!string.IsNullOrEmpty(merchantSettings.SalesChannelExclude))
                    {
                        string[] salesChannelsToExclude = merchantSettings.SalesChannelExclude.Split(',');
                        if (salesChannelsToExclude.Contains(vtexOrder.SalesChannel))
                        {
                            _context.Vtex.Logger.Debug("ProcessInvoiceHook", null, $"Order '{orderId}' skipping sales channel '{vtexOrder.SalesChannel}'");
                            return success;
                        }
                    }

                    Payments cyberTaxRequest = new Payments
                    {
                        clientReferenceInformation = new ClientReferenceInformation
                        {
                            code = vtexOrder.OrderFormId
                        },
                        taxInformation = new TaxInformation
                        {
                            //nexus = new List<string>(),
                            showTaxPerLineItem = "yes",
                            commitIndicator = true
                        },
                        orderInformation = new OrderInformation
                        {
                            //amountDetails = new AmountDetails
                            //{
                            //    currency = 
                            //},
                            billTo = new BillTo
                            {
                                address1 = vtexOrder.ShippingData.Address.Street,
                                administrativeArea = vtexOrder.ShippingData.Address.State,
                                country = this.GetCountryCode(vtexOrder.ShippingData.Address.Country),
                                postalCode = vtexOrder.ShippingData.Address.PostalCode,
                                locality = vtexOrder.ShippingData.Address.City
                            },
                            shipTo = new ShipTo
                            {
                                address1 = vtexOrder.ShippingData.Address.Street,
                                administrativeArea = vtexOrder.ShippingData.Address.State,
                                country = this.GetCountryCode(vtexOrder.ShippingData.Address.Country),
                                postalCode = vtexOrder.ShippingData.Address.PostalCode,
                                locality = vtexOrder.ShippingData.Address.City
                            },
                            lineItems = new List<LineItem>()
                        },
                    };

                    VtexDockResponse[] vtexDocks = await this.ListVtexDocks();

                    foreach (VtexOrderItem item in vtexOrder.Items)
                    {
                        string taxCode = "default";
                        string productName = string.Empty;
                        GetSkuContextResponse skuContextResponse = await this.GetSku(item.SellerSku);
                        if (skuContextResponse != null)
                        {
                            taxCode = skuContextResponse.TaxCode;
                            productName = skuContextResponse.SkuName;
                        }

                        LineItem lineItem = new LineItem
                        {
                            productSKU = item.SellerSku,
                            productCode = taxCode, //"default",
                            quantity = item.Quantity.ToString(),
                            productName = productName,
                            unitPrice = item.SellingPrice.ToString()
                        };

                        cyberTaxRequest.orderInformation.lineItems.Add(lineItem);
                    }

                    // Add shipping as an item
                    decimal shippingAmount = vtexOrder.Totals.Where(t => t.Id.Contains("Shipping")).Sum(t => t.Value) / 100;
                    LineItem lineItemShipping = new LineItem
                    {
                        productName = "Shipping",
                        productCode = "shipping",
                        productSKU = "Shipping",
                        unitPrice = shippingAmount.ToString("0.00"),
                        quantity = "1"
                    };

                    cyberTaxRequest.orderInformation.lineItems.Add(lineItemShipping);

                    PaymentsResponse taxResponse = await _cybersourceApi.CalculateTaxes(cyberTaxRequest);
                    if (taxResponse != null)
                    {
                        if (taxResponse.Status.Equals("COMPLETED"))
                        {
                            VtexTaxResponse vtexTaxResponse = await this.CybersourceResponseToVtexResponse(taxResponse);
                        }
                        else
                        {
                            _context.Vtex.Logger.Warn("ProcessInvoice", null, $"{orderId} {taxResponse.Status} {taxResponse.Reason} {taxResponse.Message}");
                            success = false;
                        }
                    }
                }
            }

            return success;
        }

        public async Task<string> ProcessConversions()
        {
            string results = string.Empty;
            StringBuilder sb = new StringBuilder();
            DateTime dtStartTime = DateTime.Now.AddDays(-1);
            DateTime dtEndTime = DateTime.Now;
            ConversionReportResponse conversionReport = await _cybersourceApi.ConversionDetailReport(dtStartTime, dtEndTime);
            if (conversionReport != null)
            {
                foreach (ConversionDetail conversionDetail in conversionReport.ConversionDetails)
                {
                    //sb.AppendLine($"{conversionDetail.MerchantReferenceNumber} {conversionDetail.OriginalDecision} - {conversionDetail.NewDecision} ");
                    sb.AppendLine(await this.UpdateOrderStatus(conversionDetail.MerchantReferenceNumber, conversionDetail.NewDecision, conversionDetail.ReviewerComments));
                }
            }

            results = sb.ToString();
            return results;
        }

        public async Task<string> UpdateOrderStatus(string merchantReferenceNumber, string newDecision, string comments)
        {
            StringBuilder results = new StringBuilder();
            VtexOrder[] vtexOrders = await this.LookupOrders(merchantReferenceNumber);
            if (vtexOrders != null)
            {
                foreach (VtexOrder vtexOrder in vtexOrders)
                {
                    if (vtexOrder != null && vtexOrder.PaymentData != null && vtexOrder.PaymentData.Transactions != null)
                    {
                        var payment = vtexOrder.PaymentData.Transactions.Select(t => t.Payments).FirstOrDefault();
                        string paymentId = payment.Select(p => p.Id).FirstOrDefault();
                        PaymentData paymentData = await _cybersourceRepository.GetPaymentData(paymentId);
                        if (paymentData != null && paymentData.CreatePaymentResponse != null)
                        {
                            if (paymentData.CreatePaymentResponse.Status.Equals(CybersourceConstants.VtexAuthStatus.Undefined))
                            {
                                bool updateStatus = true;
                                paymentData.CreatePaymentResponse.Message = comments;
                                switch (newDecision)
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

                                if (updateStatus)
                                {
                                    await _cybersourceRepository.SavePaymentData(paymentData.PaymentId, paymentData);
                                    if (paymentData.CallbackUrl != null)
                                    {
                                        SendResponse sendResponse = await this.PostCallbackResponse(paymentData.CallbackUrl, paymentData.CreatePaymentResponse);
                                        if (sendResponse != null)
                                        {
                                            results.AppendLine($"{merchantReferenceNumber} {vtexOrder.OrderId} {newDecision} updated? {sendResponse.Success}");
                                        }
                                        else
                                        {
                                            results.AppendLine($"{merchantReferenceNumber} {vtexOrder.OrderId} {newDecision} Null response.");
                                        }
                                    }
                                }
                                else
                                {
                                    results.AppendLine($"{merchantReferenceNumber} {vtexOrder.OrderId} {newDecision} No Update.");
                                }
                            }
                            else if (paymentData.CreatePaymentResponse.Status.Equals(CybersourceConstants.VtexAuthStatus.Approved) && newDecision.Equals(CybersourceConstants.CybersourceDecision.Reject))
                            {
                                // Cancel order
                                SendResponse sendResponse = await this.CancelOrder(vtexOrder.OrderId, comments);
                                results.AppendLine($"{merchantReferenceNumber} {vtexOrder.OrderId} {newDecision} canceled? {sendResponse.Success}");
                            }
                        }
                        else
                        {
                            results.AppendLine($"{merchantReferenceNumber} {vtexOrder.OrderId} {newDecision} No Payment Data.");
                        }
                    }
                    else
                    {
                        results.AppendLine($"{merchantReferenceNumber} - {newDecision} No Order Data.");
                    }
                }
            }
            else
            {
                results.AppendLine($"{merchantReferenceNumber} - {newDecision} Failed to load Order Group.");
            }

            return results.ToString();
        }

        public string GetCountryCode(string country)
        {
            string countryCode = string.Empty;
            if(!string.IsNullOrEmpty(country))
            {
                countryCode = CybersourceConstants.CountryCodesMapping[country];
            }

            return countryCode;
        }

        public async Task<VtexTaxResponse> CybersourceResponseToVtexResponse(PaymentsResponse taxResponse)
        {
            if (taxResponse == null)
            {
                return null;
            }

            if (taxResponse.OrderInformation == null)
            {
                return null;
            }

            VtexTaxResponse vtexTaxResponse = new VtexTaxResponse
            {
                Hooks = new Hook[]
                {
                    new Hook
                    {

                    }
                },
                ItemTaxResponse = new List<ItemTaxResponse>()
            };

            double totalItemTax = double.Parse(taxResponse.OrderInformation.taxAmount);

            // The last item is shipping
            LineItem shippingTaxes = taxResponse.OrderInformation.lineItems.Last();
            for (int i = 0; i < taxResponse.OrderInformation.lineItems.Count-1; i++)
            {
                LineItem lineItem = taxResponse.OrderInformation.lineItems[i];
                double itemTaxPercentOfWhole = double.Parse(lineItem.taxAmount) / totalItemTax;
                ItemTaxResponse itemTaxResponse = new ItemTaxResponse
                {
                    Id = i.ToString()
                };

                List<VtexTax> vtexTaxes = new List<VtexTax>();
                // Name = $"STATE TAX: {taxResponse.Tax.Jurisdictions.State}", // NY COUNTY TAX: MONROE
                foreach(Jurisdiction jurisdiction in lineItem.jurisdiction)
                {
                    decimal taxAmount = 0;
                    decimal.TryParse(jurisdiction.taxAmount, out taxAmount);

                    vtexTaxes.Add(
                        new VtexTax
                        {
                            Description = "",
                            Name = jurisdiction.taxName,
                            Value = taxAmount
                        }
                     );

                    double shippingAmountThisJurisdiction = 0;
                    string shippingThisJurisdiction = shippingTaxes.taxDetails.Where(t => t.type.Equals(jurisdiction.type, StringComparison.OrdinalIgnoreCase)).Select(t => t.amount).FirstOrDefault();
                    if (shippingThisJurisdiction != null && double.TryParse(shippingThisJurisdiction, out shippingAmountThisJurisdiction) && shippingAmountThisJurisdiction > 0)
                    {
                        decimal itemShippingTax = (decimal)Math.Round(shippingAmountThisJurisdiction * itemTaxPercentOfWhole, 2, MidpointRounding.ToEven);
                        vtexTaxes.Add(
                            new VtexTax
                            {
                                Description = "",
                                Name = $"{jurisdiction.taxName} (SHIPPING)",
                                Value = itemShippingTax
                            }
                         );
                    }
                }

                itemTaxResponse.Taxes = vtexTaxes.ToArray();
                vtexTaxResponse.ItemTaxResponse.Add(itemTaxResponse);
            };

            decimal totalOrderTax = decimal.Parse(taxResponse.OrderInformation.taxAmount);
            decimal totalResponseTax = vtexTaxResponse.ItemTaxResponse.SelectMany(t => t.Taxes).Sum(i => i.Value);
            if (!totalOrderTax.Equals(totalResponseTax))
            {
                decimal adjustmentAmount = Math.Round((totalOrderTax - totalResponseTax), 2, MidpointRounding.ToEven);
                int lastItemIndex = vtexTaxResponse.ItemTaxResponse.Count - 1;
                int lastTaxIndex = vtexTaxResponse.ItemTaxResponse[lastItemIndex].Taxes.Length - 1;
                vtexTaxResponse.ItemTaxResponse[lastItemIndex].Taxes[lastTaxIndex].Value += adjustmentAmount;
                //_context.Vtex.Logger.Info("CybersourceResponseToVtexResponse", null, $"Applying adjustment: {totalOrderTax} - {totalResponseTax} = {adjustmentAmount}");
            }

            //_context.Vtex.Logger.Info("CybersourceResponseToVtexResponse", null, $"Request: {JsonConvert.SerializeObject(taxResponse)}\nResponse: {JsonConvert.SerializeObject(vtexTaxResponse)}");

            return vtexTaxResponse;
        }

        public async Task<SendResponse> PostCallbackResponse(string callbackUrl, CreatePaymentResponse createPaymentResponse)
        {
            SendResponse sendResponse = null;

            if (!string.IsNullOrEmpty(callbackUrl) && createPaymentResponse != null)
            {
                callbackUrl = callbackUrl.Replace("https:", "http:", StringComparison.InvariantCultureIgnoreCase);
                try
                {
                    var jsonSerializedPaymentResponse = JsonConvert.SerializeObject(createPaymentResponse);
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(callbackUrl),
                        Content = new StringContent(jsonSerializedPaymentResponse, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
                    };

                    request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
                    string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                    if (authToken != null)
                    {
                        request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                    }

                    var client = _clientFactory.CreateClient();
                    var response = await client.SendAsync(request);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    sendResponse = new SendResponse
                    {
                        Message = responseContent,
                        StatusCode = response.StatusCode.ToString(),
                        Success = response.IsSuccessStatusCode
                    };

                }
                catch (Exception ex)
                {
                    _context.Vtex.Logger.Error("PostCallbackResponse", null, $"{callbackUrl}\n{createPaymentResponse.PaymentId} {createPaymentResponse.Status}", ex);
                }
            }

            return sendResponse;
        }

        public async Task<SendResponse> CancelOrder(string orderId, string reason)
        {
            // POST https://apiexamples.vtexcommercestable.com.br/api/oms/pvt/orders/{orderId}/cancel
            SendResponse sendResponse = null;

            try
            {
                CancelOrderRequest cancelOrderRequest = new CancelOrderRequest
                {
                    Reason = reason
                };

                string jsonSerializedData = JsonConvert.SerializeObject(cancelOrderRequest);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri($"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders/{orderId}/cancel"),
                    Content = new StringContent(jsonSerializedData, Encoding.UTF8, CybersourceConstants.APPLICATION_JSON)
                };

                request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                sendResponse = new SendResponse
                {
                    Message = responseContent,
                    StatusCode = response.StatusCode.ToString(),
                    Success = response.IsSuccessStatusCode
                };

            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CancelOrder", null, $"{orderId}", ex);
            }

            return sendResponse;
        }

        /// <summary>
        /// Limits
        /// Requests are throttled at 10 per minute with a burst allowance of 10. If you hit the speed limit the service will return a 429 http status code.
        /// </summary>
        /// <param name="bin"></param>
        /// <returns></returns>
        public async Task<BinLookup> BinLookup(string bin)
        {
            // GET https://lookup.binlist.net/{{BIN}}

            BinLookup binLookup = null;

            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://lookup.binlist.net/{bin}")
                };

                request.Headers.Add(CybersourceConstants.USE_HTTPS_HEADER_NAME, "true");
                string authToken = this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.HEADER_VTEX_CREDENTIAL];
                if (authToken != null)
                {
                    request.Headers.Add(CybersourceConstants.PROXY_AUTHORIZATION_HEADER_NAME, authToken);
                }

                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    binLookup = JsonConvert.DeserializeObject<BinLookup>(responseContent);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("BinLookup", null, null, ex);
            }

            return binLookup;
        }
    }
}
