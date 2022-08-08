using System;
using System.Threading.Tasks;
using Cybersource.Models;

namespace Cybersource.Services
{
    public interface ICybersourceApi
    {
        Task<PaymentsResponse> ProcessPayment(Payments payments, string proxyUrl, string proxyTokensUrl);
        Task<PaymentsResponse> ProcessReversal(Payments payments, string paymentId);
        Task<PaymentsResponse> ProcessCapture(Payments payments, string paymentId);
        Task<PaymentsResponse> RefundPayment(Payments payments, string paymentId);
        Task<PaymentsResponse> RefundCapture(Payments payments, string captureId);
        Task<PaymentsResponse> ProcessCredit(Payments payments);

        Task<PaymentsResponse> CreateDecisionManager(Payments payments);

        Task<PaymentsResponse> CalculateTaxes(Payments payments);

        Task<ConversionReportResponse> ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime);
        Task<string> RetrieveAvailableReports(DateTime dtStartTime, DateTime dtEndTime);
        Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime);
        Task<RetrieveTransaction> RetrieveTransaction(string transactionId);
        Task<SearchResponse> CreateSearchRequest(CreateSearchRequest createSearchRequest);

        Task<PaymentsResponse> SetupPayerAuth(Payments payments, string proxyUrl, string proxyTokensUrl);
        Task<PaymentsResponse> CheckPayerAuthEnrollment(Payments payments, string proxyUrl, string proxyTokensUrl);
        Task<PaymentsResponse> ValidateAuthenticationResults(Payments payments);

        Task<CybersourceBinLookupResponse> BinLookup(string cardNumber);
    }
}