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
using System.Net;

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
                _context.Vtex.Logger.Error("SendRequest", null, $"Error ", ex, new[] { ("method", method.ToString()), ("endpoint", endpoint), ("jsonSerializedData", jsonSerializedData) });
            }

            return sendResponse;
        }

        public async Task<VtexOrder> GetOrderInformation(string orderId, bool fromOMS = false)
        {
            string orderSource = "checkout";
            if(fromOMS)
            {
                orderSource = "oms";
            }

            VtexOrder vtexOrder = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/{orderSource}/pvt/orders/{orderId}", null);
            if (sendResponse.Success)
            {
                vtexOrder = JsonConvert.DeserializeObject<VtexOrder>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Error("GetOrderInformation", null, 
                "Error:", null,
                new[]
                {
                    ( "orderId", orderId ),
                    ( "Message", sendResponse.Message )
                });
            }

            return vtexOrder;
        }

        public async Task<VtexOrder[]> GetOrderGroup(string orderId)
        {
            VtexOrder[] vtexOrders = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/checkout/pub/orders/order-group/{orderId}", null);
            if (sendResponse.Success)
            {
                vtexOrders = JsonConvert.DeserializeObject<VtexOrder[]>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Error("GetOrderGroup", null, 
                "Error:", null,
                new[]
                {
                    ( "orderId", orderId ),
                    ( "Message", sendResponse.Message )
                });
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
                _context.Vtex.Logger.Error("LookupOrders", null, 
                "Error:", ex,
                new[]
                {
                    ( "orderId", orderId )
                });
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
                _context.Vtex.Logger.Error("GetSequence", null, 
                "Error:", ex,
                new[]
                {
                    ( "orderId", orderId )
                });
            }

            return sequence;
        }

        public async Task<string> GetOrderId(string reference, string defaultValue = null)
        {
            string orderId = reference; // default to original value
            if(!string.IsNullOrEmpty(defaultValue))
            {
                orderId = defaultValue;
            }

            try
            {
                VtexOrderList vtexOrderList = await this.SearchOrders(orderId);
                if (vtexOrderList == null || vtexOrderList.List.Count == 0)
                {
                    _context.Vtex.Logger.Warn("GetOrderId", null, $"Reference # {orderId} does not appear to be a valid VTEX order ID");
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        // defaultValue didn't work so let's try with reference
                        vtexOrderList = await this.SearchOrders(reference);
                    }
                }

                if (vtexOrderList == null || vtexOrderList.List.Count == 0)
                {
                    _context.Vtex.Logger.Warn("GetOrderId", null, $"Could not find a valid VTEX order ID for either {defaultValue} or {reference}");
                }
                else
                {
                    orderId = vtexOrderList.List.First().OrderId;
                    int charLocation = orderId.IndexOf("-", StringComparison.Ordinal);
                    if (charLocation > 0)
                    {
                        orderId = orderId.Substring(0, charLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetOrderId", null,
                "Error:", ex,
                new[]
                {
                    ( "orderId", orderId )
                });
            }

            return orderId;
        }

        public async Task<VtexOrderList> SearchOrders(string query)
        {
            VtexOrderList vtexOrderList = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders?q={query}", null);
            if (sendResponse.Success)
            {
                vtexOrderList = JsonConvert.DeserializeObject<VtexOrderList>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Error("SearchOrders", null, 
                "Error:", null,
                new[]
                {
                    ( "query", query ),
                    ( "Message", sendResponse.Message )
                });
            }

            return vtexOrderList;
        }

        public async Task<VtexDockResponse[]> ListVtexDocks()
        {
            VtexDockResponse[] listVtexDocks = null;
            try
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/logistics/pvt/configuration/docks", null);
                if (sendResponse.Success)
                {
                    listVtexDocks = JsonConvert.DeserializeObject<VtexDockResponse[]>(sendResponse.Message);
                }
             }
             catch (Exception ex)
             {
                 _context.Vtex.Logger.Error("ListVtexDocks", null, $"Error", ex);
             }

            return listVtexDocks;
        }

        public async Task<VtexDockResponse> ListDockById(string dockId)
        {
            VtexDockResponse dockResponse = null;
            try
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/logistics/pvt/configuration/docks/{dockId}", null);
                if (sendResponse.Success)
                {
                    dockResponse = JsonConvert.DeserializeObject<VtexDockResponse>(sendResponse.Message);
                 }
             }
             catch (Exception ex)
             {
                 _context.Vtex.Logger.Error("ListDockById", null, 
                 "Error:", ex,
                 new[]
                 {
                     ( "dockId", dockId )
                 });
             }

            return dockResponse;
        }

        public async Task<PickupPoints> ListPickupPoints()
        {
            PickupPoints pickupPoints = null;
            try
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://logistics.vtexcommercestable.com.br/api/logistics/pvt/configuration/pickuppoints/_search?an={this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}&pageSize=100", null);
                if (sendResponse.Success)
                {
                    pickupPoints = JsonConvert.DeserializeObject<PickupPoints>(sendResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ListPickupPoints", null, $"Error", ex);
            }

            return pickupPoints;
        }

        public async Task<GetSkuContextResponse> GetSku(string skuId)
        {
            // GET https://{accountName}.{environment}.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/skuId
            GetSkuContextResponse getSkuResponse = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/catalog_system/pvt/sku/stockkeepingunitbyid/{skuId}", null);
            if (sendResponse.Success)
            {
                getSkuResponse = JsonConvert.DeserializeObject<GetSkuContextResponse>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Error("GetSku", null, 
                "Error:", null,
                new[]
                {
                    ( "skuId", skuId ),
                    ( "Message", sendResponse.Message )
                });
            }

            return getSkuResponse;
        }

        public async Task<TransactionDetails> GetTransactionDetails(string transactionId)
        {
            TransactionDetails transactionDetails = null;
            try
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexpayments.com.br/api/pvt/transactions/{transactionId}", null);
                if (sendResponse.Success)
                {
                    transactionDetails = JsonConvert.DeserializeObject<TransactionDetails>(sendResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetTransactionDetails", null,
                "Error:", ex,
                new[]
                {
                     ( "transactionId", transactionId )
                });
            }

            return transactionDetails;
        }

        public async Task<string> InitConfiguration()
        {
            string retval = string.Empty;
            try
            {
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("InitConfiguration", null, "Error", ex);
            }

            return retval;
        }

        public async Task<string> RemoveConfiguration()
        {
            string retval = string.Empty;
            try
            {
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("RemoveConfiguration", null, "Error", ex);
            }

            return retval;
        }

        public async Task<TaxFallbackResponse> GetFallbackRate(string country, string postalCode, string provider = "avalara")
        {
            // GET https://vtexus.myvtex.com/_v/tax-fallback/{country}/{provider}/{postalCode}
            TaxFallbackResponse fallbackResponse = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://vtexus.myvtex.com/_v/tax-fallback/{country}/{provider}/{postalCode}", null);
            if (sendResponse.Success)
            {
                fallbackResponse = JsonConvert.DeserializeObject<TaxFallbackResponse>(sendResponse.Message);
            }
            else
            {
                _context.Vtex.Logger.Error("GetFallbackRate", null, 
                "Error:", null,
                new[]
                {
                    ( "country", country ),
                    ( "postalCode", postalCode ),
                    ( "provider", provider ),
                    ( "Message", sendResponse.Message )
                });
            }

            return fallbackResponse;
        }

        public async Task<VtexTaxResponse> GetFallbackTaxes(VtexTaxRequest taxRequest)
        {
            VtexTaxResponse vtexTaxResponse = null;

            try
            {
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetFallbackTaxes", null, 
                "Error:", ex,
                new[]
                {
                    ( "OrderFormId", taxRequest.OrderFormId )
                });
            }
            

            return vtexTaxResponse;
        }

        public async Task<VtexTaxResponse> GetTaxes(VtexTaxRequest taxRequest, VtexTaxRequest taxRequestOriginal)
        {
            //_context.Vtex.Logger.Debug("GetTaxes", null, $"VtexTaxRequest\n{JsonConvert.SerializeObject(taxRequest)}");

            // Combine skus
            Dictionary<string, Item> itemDictionary = new Dictionary<string, Item>();
            foreach (Item requestItem in taxRequest.Items)
            {
                if (itemDictionary.ContainsKey(requestItem.Sku))
                {
                    itemDictionary[requestItem.Sku].DiscountPrice += requestItem.DiscountPrice;
                    itemDictionary[requestItem.Sku].ItemPrice += requestItem.ItemPrice;
                    itemDictionary[requestItem.Sku].Quantity += requestItem.Quantity;
                }
                else
                {
                    itemDictionary.Add(requestItem.Sku, requestItem);
                }
            }

            taxRequest.Items = itemDictionary.Values.ToArray();

            //_context.Vtex.Logger.Debug("GetTaxes", null, "VtexTaxRequest", new[] { ("taxRequest", JsonConvert.SerializeObject(taxRequest)), ("taxRequestOriginal", JsonConvert.SerializeObject(taxRequestOriginal)) });

            VtexTaxResponse vtexTaxResponse = new VtexTaxResponse
            {
                ItemTaxResponse = new List<ItemTaxResponse>()
            };

            try
            {
                bool inNexus = await this.InNexus(taxRequest.ShippingDestination.State, taxRequest.ShippingDestination.Country);
                if (inNexus)
                {
                    Payments cyberTaxRequest = new Payments
                    {
                        clientReferenceInformation = new ClientReferenceInformation
                        {
                            code = taxRequest.OrderFormId,
                            partner = new Partner
                            {
                                solutionId = CybersourceConstants.SOLUTION_ID
                            }
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
                    // FR000000 Freight
                    // FR010000 Delivery by company vehicle
                    // FR010100 Delivery by company vehicle before passage of title
                    // FR010200 Delivery by company vehicle after passage of title
                    decimal shippingAmount = taxRequest.Totals.Where(t => t.Id.Contains("Shipping")).Sum(t => t.Value) / 100;
                    MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();

                    LineItem lineItemShipping = new LineItem
                    {
                        productName = "Shipping",
                        productCode = merchantSettings.ShippingProductCode,
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
                            vtexTaxResponse = await this.CybersourceResponseToVtexResponse(taxResponse, taxRequest);
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
                        _context.Vtex.Logger.Debug("GetTaxes", "Splitting", "VtexTaxRequest", new[] { ("taxRequest", JsonConvert.SerializeObject(taxRequest)), ("taxRequestOriginal", JsonConvert.SerializeObject(taxRequestOriginal)), ("taxResponse", JsonConvert.SerializeObject(taxResponse)), ("vtexTaxResponse", JsonConvert.SerializeObject(vtexTaxResponse)) });
                        // Split out skus to match request
                        if (taxRequest.Items.Length != taxRequestOriginal.Items.Length)
                        {
                            ItemTaxResponse[] itemTaxResponses = new ItemTaxResponse[taxRequestOriginal.Items.Length];
                            int responseId = 0;
                            decimal totalSplitAllocatedTax = 0m;
                            Dictionary<string, long> qntyAllocatedPerSku = new Dictionary<string, long>();
                            foreach (Item requestItem in taxRequestOriginal.Items)
                            {
                                Item trItem = taxRequest.Items.FirstOrDefault(i => i.Sku.Equals(requestItem.Sku));
                                string taxResponseIndexId = trItem.Id;
                                if (requestItem.Quantity == trItem.Quantity)
                                {
                                    itemTaxResponses[responseId] = vtexTaxResponse.ItemTaxResponse.FirstOrDefault(t => t.Id.Equals(taxResponseIndexId));
                                    if(itemTaxResponses[responseId] == null)
                                    {
                                        return null;
                                    }
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
                                            _context.Vtex.Logger.Warn("GetTaxes", "Splitting", $"Applying adjustment to id [{taxResponseIndexId}]: {totalTaxToAllocate} - {totalSplitAllocatedTax} = {adjustmentAmount}");
                                        }

                                        totalSplitAllocatedTax = 0m;
                                    }
                                }

                                responseId++;
                            }

                            vtexTaxResponse.ItemTaxResponse = itemTaxResponses.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Vtex.Logger.Error("TaxjarResponseToVtexResponse", "Splitting", $"Error splitting line items", ex);
                        return null;
                    }
                }
                else
                {
                    _context.Vtex.Logger.Info("TaxHandler", null, $"Order '{taxRequest.OrderFormId}' Destination state '{taxRequest.ShippingDestination.State}' is NOT in nexus");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetTaxes", "vtexTaxResponse", "Error:", ex);
            }

            return vtexTaxResponse;
        }

        public async Task<bool> InNexus(string state, string country)
        {
            bool inNexus = false;
            try
            {
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
                else
                {
                    // If no nexus is set, calculate tax
                    inNexus = true;
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("InNexus", null, 
                "Error:", ex,
                new[]
                {
                    ( "state", state ),
                    ( "country", country )
                });
            }

            return inNexus;
        }

        public async Task<bool> ProcessNotification(AllStatesNotification allStatesNotification)
        {
            bool success = true;

            try
            {
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ProcessNotification", null, 
                "Error:", ex,
                new[]
                {
                    ( "OrderId", allStatesNotification.OrderId )
                });
            }

            return success;
        }

        public async Task<bool> ProcessInvoice(string orderId)
        {
            bool success = true;
            try
            {
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
                                code = vtexOrder.OrderFormId,
                                partner = new Partner
                                {
                                    solutionId = CybersourceConstants.SOLUTION_ID
                                }
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
                        decimal shippingAmount = (decimal)vtexOrder.Totals.Where(t => t.Id.Contains("Shipping")).Sum(t => t.Value) / 100;
                        LineItem lineItemShipping = new LineItem
                        {
                            productName = "Shipping",
                            productCode = merchantSettings.ShippingProductCode,
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
                                VtexTaxResponse vtexTaxResponse = await this.CybersourceResponseToVtexResponse(taxResponse, null);
                            }
                            else
                            {
                                _context.Vtex.Logger.Warn("ProcessInvoice", null, $"{orderId} {taxResponse.Status} {taxResponse.Reason} {taxResponse.Message}");
                                success = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ProcessNotification", null, 
                "Error:", ex,
                new[]
                {
                    ( "OrderId", orderId )
                });
            }

            return success;
        }

        public async Task<string> ProcessConversions()
        {
            string results = string.Empty;

            try
            {
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ProcessConversions", null, "Error:", ex);
            }

            return results;
        }

        public async Task<string> UpdateOrderStatus(string merchantReferenceNumber, string newDecision, string comments)
        {
            StringBuilder results = new StringBuilder();
            
            try
            {
                MerchantSettings merchantSettings = await _cybersourceRepository.GetMerchantSettings();
                if (!string.IsNullOrEmpty(merchantSettings.OrderSuffix) && merchantReferenceNumber.EndsWith(merchantSettings.OrderSuffix.Trim()))
                {
                    // Remove custom order suffix
                    merchantReferenceNumber = merchantReferenceNumber.Substring(0, merchantReferenceNumber.LastIndexOf(merchantSettings.OrderSuffix.Trim()));
                }

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
                                        if (paymentData.CreatePaymentRequest.CallbackUrl != null)
                                        {
                                            SendResponse sendResponse = await this.PostCallbackResponse(paymentData.CreatePaymentRequest.CallbackUrl, paymentData.CreatePaymentResponse);
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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("UpdateOrderStatus", null, 
                "Error:", ex,
                new[]
                {
                    ( "merchantReferenceNumber", merchantReferenceNumber ),
                    ( "newDecision", newDecision ),
                    ( "comments", comments )
                });
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

        public async Task<VtexTaxResponse> CybersourceResponseToVtexResponse(PaymentsResponse taxResponse, VtexTaxRequest taxRequest)
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

            try
            {
                if(taxRequest == null)
                {
                    taxRequest = new VtexTaxRequest
                    {
                        Items = new Item[taxResponse.OrderInformation.lineItems.Count]
                    };

                    for (int index = 0; index < taxResponse.OrderInformation.lineItems.Count - 1; index++)
                    {
                        taxRequest.Items[index].Id = index.ToString();
                    }
                }

                double totalItemTax = double.Parse(taxResponse.OrderInformation.taxAmount);

                // The last item is shipping
                LineItem shippingTaxes = taxResponse.OrderInformation.lineItems.Last();
                int i = 0;
                foreach (Item item in taxRequest.Items)
                {
                    LineItem lineItem = taxResponse.OrderInformation.lineItems[i];
                    double itemTaxPercentOfWhole = double.Parse(lineItem.taxAmount) / totalItemTax;
                    ItemTaxResponse itemTaxResponse = new ItemTaxResponse
                    {
                        Id = item.Id
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
                    i++;
                }

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
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("CybersourceResponseToVtexResponse", null, 
                "Error:", ex,
                new[]
                {
                    ( "taxRequest", JsonConvert.SerializeObject(taxRequest) ),
                    ( "taxResponse", JsonConvert.SerializeObject(taxResponse) )
                });
            }

            return vtexTaxResponse;
        }

        public async Task<SendResponse> PostCallbackResponse(string callbackUrl, CreatePaymentResponse createPaymentResponse)
        {
            SendResponse sendResponse = null;

            try
            {
                if (!string.IsNullOrEmpty(callbackUrl) && createPaymentResponse != null)
                {
                    callbackUrl = callbackUrl.Replace("https:", "http:", StringComparison.InvariantCultureIgnoreCase);
                    var jsonSerializedPaymentResponse = JsonConvert.SerializeObject(createPaymentResponse);
                    sendResponse = await this.SendRequest(HttpMethod.Post, callbackUrl, jsonSerializedPaymentResponse);
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("PostCallbackResponse", null, 
                "Error:", ex,
                new[]
                {
                    ( "callbackUrl", callbackUrl ),
                    ( "createPaymentResponse", JsonConvert.SerializeObject(createPaymentResponse) )
                });
            }

            return sendResponse;
        }

        public async Task<SendResponse> CancelOrder(string orderId, string reason)
        {
            // POST https://apiexamples.vtexcommercestable.com.br/api/oms/pvt/orders/{orderId}/cancel
            CancelOrderRequest cancelOrderRequest = new CancelOrderRequest
            {
                Reason = reason
            };

            string json = JsonConvert.SerializeObject(cancelOrderRequest);
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Post, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders/{orderId}/cancel", json);

            return sendResponse;
        }

        public async Task<VtexBinLookup> VtexBinLookup(string bin)
        {
            VtexBinLookup binLookup = null;
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"https://api.vtexpayments.com.br/api/pvt/bins/{bin}", null);
            if (sendResponse.Success)
            {
                binLookup = JsonConvert.DeserializeObject<VtexBinLookup>(sendResponse.Message);
            }

            return binLookup;
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
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"https://lookup.binlist.net/{bin}", null);
            if (sendResponse.Success)
            {
                binLookup = JsonConvert.DeserializeObject<BinLookup>(sendResponse.Message);
            }

            return binLookup;
        }

        public async Task<List<string>> GetPropertyList()
        {
            PaymentRequestWrapper requestWrapper = new PaymentRequestWrapper(new CreatePaymentRequest());
            List<string> propertyList = requestWrapper.GetPropertyList();
            try
            {
                string jsonSerializedOrderConfig = await _cybersourceRepository.GetOrderConfiguration();
                if(!string.IsNullOrEmpty(jsonSerializedOrderConfig))
                {
                    List<CustomApp> customApps = null;
                    dynamic orderConfig = JsonConvert.DeserializeObject(jsonSerializedOrderConfig);
                    try
                    {
                        string appsConfig = JsonConvert.SerializeObject(orderConfig["apps"]);
                        if (!string.IsNullOrEmpty(appsConfig))
                        {
                            customApps = JsonConvert.DeserializeObject<List<CustomApp>>(appsConfig);
                            foreach (CustomApp customApp in customApps)
                            {
                                JArray fieldsArray = (JArray)customApp.Fields;
                                List<string> fieldNames = fieldsArray.ToObject<List<string>>();
                                foreach (string fieldName in fieldNames)
                                {
                                    propertyList.Add($"CustomData.CustomApps.{customApp.Id}_{fieldName}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.Vtex.Logger.Error("GetPropertyList", null, "Error getting property list", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("GetPropertyList", null, "Error:", ex);
            }

            return propertyList;
        }

        public async Task<ValidatedUser> ValidateUserToken(string token)
        {
            ValidatedUser validatedUser = null;
            ValidateToken validateToken = new ValidateToken
            {
                Token = token
            };

            var jsonSerializedToken = JsonConvert.SerializeObject(validateToken);

            try
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Post, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.vtexcommercestable.com.br/api/vtexid/credential/validate", jsonSerializedToken);
                if (sendResponse.Success)
                {
                    validatedUser = JsonConvert.DeserializeObject<ValidatedUser>(sendResponse.Message);
                }
                else
                {
                    _context.Vtex.Logger.Warn("ValidateUserToken", null, $"Error validating user token: [{sendResponse.StatusCode}] '{sendResponse.Message}'");
                }
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("ValidateUserToken", null, $"Error validating user token", ex);
            }

            return validatedUser;
        }

        public async Task<HttpStatusCode> IsValidAuthUser()
        {
            if (string.IsNullOrEmpty(_context.Vtex.AdminUserAuthToken))
            {
                return HttpStatusCode.Unauthorized;
            }

            ValidatedUser validatedUser = null;

            try {
                validatedUser = await ValidateUserToken(_context.Vtex.AdminUserAuthToken);
            }
            catch (Exception ex)
            {
                _context.Vtex.Logger.Error("IsValidAuthUser", null, "Error fetching user", ex);

                return HttpStatusCode.BadRequest;
            }

            bool hasPermission = validatedUser != null && validatedUser.AuthStatus.Equals("Success");

            if (!hasPermission)
            {
                _context.Vtex.Logger.Warn("IsValidAuthUser", null, "User Does Not Have Permission");

                return HttpStatusCode.Forbidden;
            }

            return HttpStatusCode.OK;
        }

        public async Task<PersonalData> GetPersonalData(string userProfileId)
        {

            PersonalData personalData = null;
            if (string.IsNullOrEmpty(userProfileId))
            {
                _context.Vtex.Logger.Warn("GetPersonalData", null, "User Profile Id is Empty.");
            }
            else
            {
                SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/profile-system/pvt/profiles/{userProfileId}/personalData", null);
                if (sendResponse.Success)
                {
                    personalData = JsonConvert.DeserializeObject<PersonalData>(sendResponse.Message);
                }
                else
                {
                    _context.Vtex.Logger.Warn("GetPersonalData", null, $"User Profile Id: {userProfileId} Error: [{sendResponse.StatusCode}] '{sendResponse.Message}'");
                }
            }

            return personalData;
        }

        public async Task<VtexOrderList> ListOrders(string queryString)
        {
            VtexOrderList vtexOrderList = new VtexOrderList();
            SendResponse sendResponse = await this.SendRequest(HttpMethod.Get, $"http://{this._httpContextAccessor.HttpContext.Request.Headers[CybersourceConstants.VTEX_ACCOUNT_HEADER_NAME]}.{CybersourceConstants.ENVIRONMENT}.com.br/api/oms/pvt/orders?{queryString}", null);

            if (sendResponse.Success)
            {
                vtexOrderList = JsonConvert.DeserializeObject<VtexOrderList>(sendResponse.Message);
            }

            return vtexOrderList;
        }

        public async Task<VtexOrderList> ListOrdersForShopperId(string shopperId)
        {
            return await this.ListOrders($"orderBy=creationDate,asc&q={shopperId}");
        }
    }
}
