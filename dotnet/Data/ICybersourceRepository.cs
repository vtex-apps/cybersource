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
    }
}