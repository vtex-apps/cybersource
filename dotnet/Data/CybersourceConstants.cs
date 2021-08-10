using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Data
{
    public class CybersourceConstants
    {
        public const string AppName = "cybersource";

        public const string FORWARDED_HEADER = "X-Forwarded-For";
        public const string FORWARDED_HOST = "X-Forwarded-Host";
        public const string APPLICATION_JSON = "application/json";
        public const string APPLICATION_FORM = "application/x-www-form-urlencoded";
        public const string HEADER_VTEX_CREDENTIAL = "X-Vtex-Credential";
        public const string AUTHORIZATION_HEADER_NAME = "Authorization";
        public const string PROXY_AUTHORIZATION_HEADER_NAME = "Proxy-Authorization";
        public const string USE_HTTPS_HEADER_NAME = "X-Vtex-Use-Https";
        public const string PROXY_TO_HEADER_NAME = "X-Vtex-Proxy-To";
        public const string VTEX_ACCOUNT_HEADER_NAME = "X-Vtex-Account";
        public const string ENVIRONMENT = "vtexcommercestable";
        public const string LOCAL_ENVIRONMENT = "myvtex";
        public const string VTEX_ID_HEADER_NAME = "VtexIdclientAutCookie";
        public const string HEADER_VTEX_WORKSPACE = "X-Vtex-Workspace";
        public const string APP_SETTINGS = "vtex.cybersource";
        public const string ACCEPT = "Accept";
        public const string CONTENT_TYPE = "Content-Type";
        public const string MINICART = "application/vnd.vtex.checkout.minicart.v1+json";
        public const string HTTP_FORWARDED_HEADER = "HTTP_X_FORWARDED_FOR";
        public const string API_VERSION_HEADER = "'x-api-version";
        public const string TAXJAR_API_VERSION = "2020-08-07";
        public const string BUCKET_PAYMENT = "payments";
        public const string BUCKET_ANTIFRAUD = "antifraud";
        public const string BUCKET_TOKEN = "cybersource-token";
        public const string TOKEN_TEST = "oauth-token-test";
        public const string TOKEN_LIVE = "oauth-token-live";
        public const string CACHE_BUCKET = "tax-cache";

        public const string PAYMENTS = "/pts/v2/";
        public const string RISK = "/risk/v1/";

        public class VtexAuthStatus
        {
            public const string Approved = "approved";
            public const string Denied = "denied";
            public const string Undefined = "undefined";
        }

        public class VtexAntifraudStatus
        {
            public const string Received = "received";
            public const string Approved = "approved";
            public const string Denied = "denied";
            public const string Undefined = "undefined";
        }

        public class VtexAntifraudType
        {
            public const string Automatic = "automatic";
            public const string Manual = "manual ";
        }

        public class VtexOrderStatus
        {
            public const string OrderCreated = "order-created";
            public const string OrderCompleted = "order-completed";
            public const string OnOrderCompleted = "on-order-completed";
            public const string PaymentPending = "payment-pending";
            public const string WaitingForOrderAuthorization = "waiting-for-order-authorization";
            public const string ApprovePayment = "approve-payment";
            public const string PaymentApproved = "payment-approved";
            public const string PaymentDenied = "payment-denied";
            public const string RequestCancel = "request-cancel";
            public const string WaitingForSellerDecision = "waiting-for-seller-decision";
            public const string AuthorizeFullfilment = "authorize-fulfillment";
            public const string OrderCreateError = "order-create-error";
            public const string OrderCreationError = "order-creation-error";
            public const string WindowToCancel = "window-to-cancel";
            public const string ReadyForHandling = "ready-for-handling";
            public const string StartHanding = "start-handling";
            public const string Handling = "handling";
            public const string InvoiceAfterCancellationDeny = "invoice-after-cancellation-deny";
            public const string OrderAccepted = "order-accepted";
            public const string Invoice = "invoice";
            public const string Invoiced = "invoiced";
            public const string Replaced = "replaced";
            public const string CancellationRequested = "cancellation-requested";
            public const string Cancel = "cancel";
            public const string Canceled = "canceled";
            public const string Cancelled = "cancelled";
        }

        public class Domain
        {
            public const string Fulfillment = "Fulfillment";
            public const string Marketplace = "Marketplace";
        }

        public const string ResponseHeadersSeparator = "\r\n";
        public const string SandboxApiEndpoint = "apitest.cybersource.com";
        public const string ProductionApiEndpoint = "api.cybersource.com";
        public const string SignatureAlgorithm = "HmacSHA256";

        public const string AUTH_SITE_BASE = "googleauth.myvtex.com";
        public const string REDIRECT_PATH = "return";
        public const string AUTH_APP_PATH = "cybersource";
        public const string AUTH_PATH = "auth";
        public const string REFRESH_PATH = "refresh-token";

        public const string PROXY_HEADER_PREFIX = "X-PROVIDER-Forward-";
        public const string PROXY_FORWARD_TO = "X-PROVIDER-Forward-To";

        public const string INSTALLMENT = "install";
        public const string INSTALLMENT_INTERNET = "install_internet";
        public const string INTERNET = "internet";

        public class Regions
        {
            public const string Colombia = "CO";
            public const string Peru = "PE";
            public const string Mexico = "MX";
            public const string Brasil = "BR";
        }

        public class Processors
        {
            public const string Braspag = "Braspag";
            public const string VPC = "VPC";
            public const string Izipay = "Izipay";
            public const string eGlobal = "eGlobal";
            public const string BBVA = "BBVA";
            public const string Banorte = "Banorte";
            public const string Prosa = "Prosa";
            public const string Santander = "Santander";
            public const string AmexDirect = "Amex Direct";
        }
    }
}
