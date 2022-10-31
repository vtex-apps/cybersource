namespace Cybersource.Models
{
    public class PaymentData
    {
        public string TransactionId { get; set; }
        public string PaymentId { get; set; }
        public decimal Value { get; set; }
        public string RequestId { get; set; }
        public string AuthorizationId { get; set; }
        public string CaptureId { get; set; }
        public CreatePaymentResponse CreatePaymentResponse { get; set; }
        public CreatePaymentRequest CreatePaymentRequest { get; set; }
        public bool ImmediateCapture { get; set; }
        public string PayerAuthReferenceId { get; set; }
        public string AuthenticationTransactionId { get; set; }
    }
}
