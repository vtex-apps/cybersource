using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cybersource.Models;

namespace Cybersource.Services
{
    public interface ICybersourcePaymentService
    {
        Task<(CreatePaymentResponse, PaymentsResponse)> CreatePayment(CreatePaymentRequest createPaymentRequest, string authenticationTransactionId = null, ConsumerAuthenticationInformation consumerAuthenticationInformation = null);
        Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest cancelPaymentRequest);
        Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest);
        Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest);

        Task<CreatePaymentResponse> SetupPayerAuth(CreatePaymentRequest createPaymentRequest);
        Task<PaymentsResponse> CheckPayerAuthEnrollment(PaymentData paymentData);
        Task<PaymentsResponse> ValidateAuthenticationResults(CreatePaymentRequest createPaymentRequest, string authenticationTransactionId);

        Task<(CreatePaymentResponse createPaymentResponse, PaymentsResponse paymentsResponse, string paymentStatus, bool doCancel)> GetPaymentStatus(CreatePaymentResponse createPaymentResponse, CreatePaymentRequest createPaymentRequest, PaymentsResponse paymentsResponse, bool isPayerAuth);

        Task<SendAntifraudDataResponse> SendAntifraudData(SendAntifraudDataRequest sendAntifraudDataRequest);
        Task<SendAntifraudDataResponse> GetAntifraudStatus(string id);

        Task<List<MerchantDefinedInformation>> GetMerchantDefinedInformation(MerchantSettings merchantSettings, PaymentRequestWrapper requestWrapper);

        Task<ConversionReportResponse> ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime);
        Task<ConversionReportResponse> ConversionDetailReport(string startTime, string endTime);
        Task<string> RetrieveAvailableReports(DateTime dtStartTime, DateTime dtEndTime);
        Task<string> RetrieveAvailableReports(string startTime, string endTime);
        Task<string> GetPurchaseAndRefundDetails(DateTime dtStartTime, DateTime dtEndTime);
        Task<string> GetPurchaseAndRefundDetails(string startTime, string endTime);
        Task<string> GetAuthUrl();
        Task<RetrieveTransaction> RetrieveTransaction(string requestId);
    }
}