using Newtonsoft.Json;

namespace Cybersource.Models
{
    public class CreatePaymentResponse
    {
        /// <summary>
        /// The same paymentId sent in the request
        /// </summary>
        [JsonProperty("paymentId")]
        public string PaymentId { get; set; }

        /// <summary>
        /// Provider's payment status:
        /// • approved
        /// • denied
        /// • undefined
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Provider's unique identifier for the transaction
        /// </summary>
        [JsonProperty("tid")]
        public string Tid { get; set; }

        /// <summary>
        /// Provider's unique identifier for the authorization
        /// </summary>
        [JsonProperty("authorizationId")]
        public string AuthorizationId { get; set; }

        /// <summary>
        /// Provider's unique sequential number for the transaction
        /// </summary>
        [JsonProperty("nsu")]
        public string Nsu { get; set; }

        /// <summary>
        /// Acquirer name (mostly used for card payments)
        /// </summary>
        [JsonProperty("acquirer")]
        public string Acquirer { get; set; }

        /// <summary>
        /// The bank invoice URL to be presented to the end user,
        /// or the URL the end user needs to be redirected to (external authentication, 3DS, etc.)
        /// </summary>
        [JsonProperty("paymentUrl")]
        public string PaymentUrl { get; set; }

        /// <summary>
        /// The bank invoice unformatted identification number
        /// </summary>
        [JsonProperty("identificationNumber")]
        public string IdentificationNumber { get; set; }

        /// <summary>
        /// The bank invoice formatted identification number that will be presented to the end user
        /// </summary>
        [JsonProperty("identificationNumberFormatted")]
        public string IdentificationNumberFormatted { get; set; }

        /// <summary>
        /// The bank invoice barcode image type: 
        /// • i25 for Brazilian Boleto Bancário
        /// </summary>
        [JsonProperty("barCodeImageType")]
        public string BarCodeImageType { get; set; }

        /// <summary>
        /// The bank invoice number to generate a barcode (must follow any regulations/specifications for targeted countries)
        /// </summary>
        [JsonProperty("barCodeImageNumber")]
        public string BarCodeImageNumber { get; set; }

        /// <summary>
        /// Provider's operation/error code to be logged
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Provider's operation/error message to be logged
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Total time (in seconds) before we make and automatic call to /settlements no mather if the payment was approved by merchant's antifraud or not
        /// </summary>
        [JsonProperty("delayToAutoSettle")]
        public long DelayToAutoSettle { get; set; }

        /// <summary>
        /// Total time (in seconds) before we make and automatic call to /settlements after merchant's antifraud approval
        /// </summary>
        [JsonProperty("delayToAutoSettleAfterAntifraud")]
        public long DelayToAutoSettleAfterAntifraud { get; set; }

        /// <summary>
        /// Total time (in seconds) to wait for an authorization and make and automatic call to /cancellations to cancel the payment
        /// </summary>
        [JsonProperty("delayToCancel")]
        public long DelayToCancel { get; set; }

        /// <summary>
        /// Indicate the app that will handle the payment flow at Checkout
        /// </summary>
        [JsonProperty("paymentAppData")]
        public PaymentAppData PaymentAppData { get; set; }
    }

    public class PaymentAppData
    {
        [JsonProperty("appName")]
        public string AppName { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }
}
