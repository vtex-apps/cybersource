using Cybersource.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cybersource.Data
{
    public interface ICybersourceRepository
    {
        Task<MerchantSettings> GetMerchantSettings();

        Task<PaymentData> GetPaymentData(string paymentIdentifier);
        Task SavePaymentData(string paymentIdentifier, PaymentData paymentData);

        Task<SendAntifraudDataResponse> GetAntifraudData(string id);
        Task SaveAntifraudData(string id, SendAntifraudDataResponse antifraudDataResponse);

        Task<CybersourceToken> LoadToken(bool isProduction);
        Task<bool> SaveToken(CybersourceToken token, bool isProduction);
    }
}