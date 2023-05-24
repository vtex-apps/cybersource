using System;
using System.Threading.Tasks;
using Cybersource.Models;

namespace Cybersource.Services
{
    public interface ICybersourceApi
    {
        Task<PaymentsResponse> ProcessPayment(Payments payments, string proxyUrl, string proxyTokensUrl, MerchantSettings merchantSettings);
        Task<PaymentsResponse> ProcessReversal(Payments payments, string paymentId, MerchantSettings merchantSettings);
        Task<PaymentsResponse> ProcessCapture(Payments payments, string paymentId, MerchantSettings merchantSettings);
        Task<PaymentsResponse> RefundPayment(Payments payments, string paymentId, MerchantSettings merchantSettings);
        Task<PaymentsResponse> RefundCapture(Payments payments, string captureId, MerchantSettings merchantSettings);
        Task<PaymentsResponse> ProcessCredit(Payments payments, MerchantSettings merchantSettings);

        Task<PaymentsResponse> CreateDecisionManager(Payments payments, MerchantSettings merchantSettings);

        Task<PaymentsResponse> CalculateTaxes(Payments payments, MerchantSettings merchantSettings);

        Task<ConversionReportResponse> ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime);
        Task<string> RetrieveAvailableReports(DateTime dtStartTime, DateTime dtEndTime, MerchantSettings merchantSettings);
        Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime, MerchantSettings merchantSettings);
        Task<RetrieveTransaction> RetrieveTransaction(string transactionId, MerchantSettings merchantSettings);
        Task<SearchResponse> CreateSearchRequest(CreateSearchRequest createSearchRequest, MerchantSettings merchantSettings);

        Task<PaymentsResponse> SetupPayerAuth(Payments payments, string proxyUrl, string proxyTokensUrl, MerchantSettings merchantSettings);
        Task<PaymentsResponse> CheckPayerAuthEnrollment(Payments payments, string proxyUrl, string proxyTokensUrl, MerchantSettings merchantSettings);
        Task<PaymentsResponse> ValidateAuthenticationResults(Payments payments, string proxyUrl, string proxyTokensUrl, MerchantSettings merchantSettings);

        Task<CybersourceBinLookupResponse> BinLookup(string cardNumber, MerchantSettings merchantSettings);
    }
}