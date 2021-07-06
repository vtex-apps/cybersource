namespace service.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Cybersource.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Cybersource.Models;
    using Newtonsoft.Json;

    public class RoutesController : Controller
    {
        private readonly ICybersourcePaymentService _cybersourcePaymentService;

        public RoutesController(ICybersourcePaymentService cybersourcePaymentService)
        {
            this._cybersourcePaymentService = cybersourcePaymentService ?? throw new ArgumentNullException(nameof(cybersourcePaymentService));
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments
        /// Creates a new payment and/or initiates the payment flow.
        /// </summary>
        /// <param name="createPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest createPaymentRequest)
        {
            Console.WriteLine("    - CreatePayment -     ");
            //Console.WriteLine($"    - CreatePaymentRequest: {JsonConvert.SerializeObject(createPaymentRequest)} -     ");
            var paymentResponse = await this._cybersourcePaymentService.CreatePayment(createPaymentRequest);

            Response.Headers.Add("Cache-Control", "private");

            return Json(paymentResponse);
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/cancellations
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="cancelPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CancelPayment(string paymentId, [FromBody] CancelPaymentRequest cancelPaymentRequest)
        {
            var cancelResponse = await this._cybersourcePaymentService.CancelPayment(cancelPaymentRequest);

            return Json(cancelResponse);
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/settlements
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="capturePaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> CapturePayment(string paymentId, [FromBody] CapturePaymentRequest capturePaymentRequest)
        {
            Console.WriteLine($"    - CapturePayment -   {paymentId}  ");
            var captureResponse = await this._cybersourcePaymentService.CapturePayment(capturePaymentRequest);

            return Json(captureResponse);
        }

        /// <summary>
        /// https://{{providerApiEndpoint}}/payments/{{paymentId}}/refunds
        /// </summary>
        /// <param name="paymentId">VTEX payment ID from this payment</param>
        /// <param name="refundPaymentRequest"></param>
        /// <returns></returns>
        public async Task<IActionResult> RefundPayment(string paymentId, [FromBody] RefundPaymentRequest refundPaymentRequest)
        {
            var refundResponse = await this._cybersourcePaymentService.RefundPayment(refundPaymentRequest);

            return Json(refundResponse);
        }

        public JsonResult PaymentMethods()
        {
            PaymentMethodsList methods = new PaymentMethodsList();
            methods.PaymentMethods = new System.Collections.Generic.List<string>();
            methods.PaymentMethods.Add("Visa");
            methods.PaymentMethods.Add("Mastercard");
            methods.PaymentMethods.Add("American Express");
            methods.PaymentMethods.Add("Discover");
            methods.PaymentMethods.Add("JCB");
            methods.PaymentMethods.Add("Diners");

            //Response.Headers.Add("Cache-Control", "private");

            return Json(methods);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/transactions
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> SendAntifraudData()
        {
            SendAntifraudDataResponse sendAntifraudDataResponse = null;
            if ("post".Equals(HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                string bodyAsText = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                SendAntifraudDataRequest sendAntifraudDataRequest = JsonConvert.DeserializeObject<SendAntifraudDataRequest>(bodyAsText);
                sendAntifraudDataResponse = await this._cybersourcePaymentService.SendAntifraudData(sendAntifraudDataRequest);
            }

            return Json(sendAntifraudDataResponse);
        }

        /// <summary>
        /// http://{{providerApiEndpoint}}/transactions/transactions.id
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> GetAntifraudStatus(string transactionId)
        {
            var getAntifraudStatusResponse = await this._cybersourcePaymentService.GetAntifraudStatus(transactionId);

            return Json(getAntifraudStatusResponse);
        }
    }
}
