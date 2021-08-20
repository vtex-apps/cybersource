﻿using Cybersource.Models;
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
    }
}