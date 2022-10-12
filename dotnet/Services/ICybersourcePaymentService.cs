using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cybersource.Models;

namespace Cybersource.Services
{
    public interface ICybersourcePaymentService
    {
        Task<(CreatePaymentResponse, PaymentsResponse)> CreatePayment(CreatePaymentRequest createPaymentRequest);
        Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest cancelPaymentRequest);
        Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest);
        Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest);

        Task<CreatePaymentResponse> SetupPayerAuth(CreatePaymentRequest createPaymentRequest);
        Task<PaymentsResponse> CheckPayerAuthEnrollment(CreatePaymentRequest createPaymentRequest);

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
    }
}