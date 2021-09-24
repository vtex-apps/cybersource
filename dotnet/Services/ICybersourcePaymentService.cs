using System;
using System.Threading.Tasks;
using Cybersource.Models;

namespace Cybersource.Services
{
    public interface ICybersourcePaymentService
    {
        Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest);
        Task<CancelPaymentResponse> CancelPayment(CancelPaymentRequest cancelPaymentRequest);
        Task<CapturePaymentResponse> CapturePayment(CapturePaymentRequest capturePaymentRequest);
        Task<RefundPaymentResponse> RefundPayment(RefundPaymentRequest refundPaymentRequest);

        Task<SendAntifraudDataResponse> SendAntifraudData(SendAntifraudDataRequest sendAntifraudDataRequest);
        Task<SendAntifraudDataResponse> GetAntifraudStatus(string id);

        Task ConversionDetailReport(DateTime dtStartTime, DateTime dtEndTime);
        Task ConversionDetailReport(string atartTime, string endTime);

        Task<string> GetAuthUrl();
    }
}