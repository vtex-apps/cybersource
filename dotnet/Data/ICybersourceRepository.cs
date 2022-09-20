﻿using Cybersource.Models;
using System.Threading.Tasks;

namespace Cybersource.Data
{
    public interface ICybersourceRepository
    {
        Task<MerchantSettings> GetMerchantSettings();
        Task<bool> SetMerchantSettings(MerchantSettings merchantSettings);

        Task<PaymentData> GetPaymentData(string paymentIdentifier);
        Task SavePaymentData(string paymentIdentifier, PaymentData paymentData);

        Task<CreatePaymentRequest> GetCreatePaymentRequest(string id);
        Task SaveCreatePaymentRequest(string id, CreatePaymentRequest createPaymentRequest);

        Task<SendAntifraudDataResponse> GetAntifraudData(string id);
        Task SaveAntifraudData(string id, SendAntifraudDataResponse antifraudDataResponse);

        bool TryGetCache(int cacheKey, out VtexTaxResponse vtexTaxResponse);
        Task<bool> SetCache(int cacheKey, VtexTaxResponse vtexTaxResponse);

        Task<string> GetOrderConfiguration();
        Task<bool> SetOrderConfiguration(string jsonSerializedOrderConfig);
    }
}