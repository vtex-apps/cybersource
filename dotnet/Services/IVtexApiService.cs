using Cybersource.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cybersource.Services
{
    public interface IVtexApiService
    {
        Task<PickupPoints> ListPickupPoints();
        Task<TaxFallbackResponse> GetFallbackRate(string country, string postalCode, string provider = "avalara");
        Task<GetSkuContextResponse> GetSku(string skuId);
        Task<VtexDockResponse[]> ListVtexDocks();
        Task<string> RemoveConfiguration();
        Task<string> InitConfiguration();
        Task<VtexTaxResponse> CybersourceResponseToVtexResponse(PaymentsResponse taxResponse);
        Task<bool> ProcessNotification(AllStatesNotification allStatesNotification);
        Task<VtexTaxResponse> GetTaxes(VtexTaxRequest taxRequest, VtexTaxRequest taxRequestOriginal);
        Task<VtexTaxResponse> GetFallbackTaxes(VtexTaxRequest taxRequest);

        Task<SendResponse> PostCallbackResponse(string callbackUrl, CreatePaymentResponse createPaymentResponse);
        Task<string> ProcessConversions();
        Task<string> UpdateOrderStatus(string merchantReferenceNumber, string newDecision, string comments);
        Task<VtexOrder> GetOrderInformation(string orderId);
        Task<VtexOrder[]> GetOrderGroup(string orderId);
        Task<VtexOrder[]> LookupOrders(string orderId);
        Task<string> GetSequence(string orderId);
        Task<string> GetOrderId(string reference);
        Task<List<string>> GetPropertyList();

        Task<BinLookup> BinLookup(string bin);
    }
}