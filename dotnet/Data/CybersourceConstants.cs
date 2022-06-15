using System.Collections.Generic;

namespace Cybersource.Data
{
    public class CybersourceConstants
    {
        public const string AppName = "cybersource";

        public const string SOLUTION_ID = "H4D4KDID";
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
        public const string APP_SETTINGS = "vtex.cybersource-ui";
        public const string ACCEPT = "Accept";
        public const string CONTENT_TYPE = "Content-Type";
        public const string MINICART = "application/vnd.vtex.checkout.minicart.v1+json";
        public const string HTTP_FORWARDED_HEADER = "HTTP_X_FORWARDED_FOR";
        public const string API_VERSION_HEADER = "'x-api-version";
        public const string BUCKET_PAYMENT = "payments";
        public const string BUCKET_ANTIFRAUD = "antifraud";
        public const string BUCKET_TOKEN = "cybersource-token";
        public const string TOKEN_TEST = "oauth-token-test";
        public const string TOKEN_LIVE = "oauth-token-live";
        public const string CACHE_BUCKET = "tax-cache";
        public const string SIGNATURE_ENCODING = "base64";
        public const string SIGNATURE_KEY_FORMAT = "base64";

        public const string PAYMENTS = "/pts/v2/";
        public const string RISK = "/risk/v1/";
        public const string TAX = "/vas/v2/";
        public const string REPORTING = "/reporting/v3/";
        public const string TRANSACTIONS = "/tss/v2/";
        public const string BIN = "/bin/v1/";

        public static class CybersourceDecision
        {
            public const string Review = "REVIEW";
            public const string Reject = "REJECT";
            public const string Accept = "ACCEPT";
        }

        public static class VtexAuthStatus
        {
            public const string Approved = "approved";
            public const string Denied = "denied";
            public const string Undefined = "undefined";
        }

        public static class VtexAntifraudStatus
        {
            public const string Received = "received";
            public const string Approved = "approved";
            public const string Denied = "denied";
            public const string Undefined = "undefined";
        }

        public static class VtexAntifraudType
        {
            public const string Automatic = "automatic";
            public const string Manual = "manual ";
        }

        public static class VtexOrderStatus
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

        public static class Domain
        {
            public const string Fulfillment = "Fulfillment";
            public const string Marketplace = "Marketplace";
        }

        public static class ManifestCustomField
        {
            public const string CompanyName = "Company Name";
            public const string CompanyTaxId = "Company Tax Id";
            public const string CaptureSetting = "Capture Setting";
            public const string DelayedCapture = "Delayed Capture";
            public const string ImmediateCapture = "Immediate Capture";
            public const string MerchantId = "Merchant Id";
            public const string MerchantKey = "Merchant Key";
            public const string SharedSecretKey = "Shared Secret Key";
        }

        public static class CaptureSetting
        {
            public const string DelayedCapture = "DelayedCapture";
            public const string ImmediateCapture = "AuthAndCapture";
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

        public static class Regions
        {
            public const string Colombia = "CO";
            public const string Peru = "PE";
            public const string Mexico = "MX";
            public const string Brasil = "BR";
        }

        public static class Processors
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

        public enum CardType
        {
            Unknown,
            MasterCard,
            Visa,
            AmericanExpress,
            Discover,
            JCB,
            Diners,
            Elo,
            Hipercard
        }

        internal static readonly Dictionary<string, string> CountryCodesMapping = new Dictionary<string, string>()
        {
           { "AFG", "AF" },    // Afghanistan
           { "ALB", "AL" },    // Albania
           { "ARE", "AE" },    // U.A.E.
           { "ARG", "AR" },    // Argentina
           { "ARM", "AM" },    // Armenia
           { "AUS", "AU" },    // Australia
           { "AUT", "AT" },    // Austria
           { "AZE", "AZ" },    // Azerbaijan
           { "BEL", "BE" },    // Belgium
           { "BGD", "BD" },    // Bangladesh
           { "BGR", "BG" },    // Bulgaria
           { "BHR", "BH" },    // Bahrain
           { "BIH", "BA" },    // Bosnia and Herzegovina
           { "BLR", "BY" },    // Belarus
           { "BLZ", "BZ" },    // Belize
           { "BOL", "BO" },    // Bolivia
           { "BRA", "BR" },    // Brazil
           { "BRN", "BN" },    // Brunei Darussalam
           { "CAN", "CA" },    // Canada
           { "CHE", "CH" },    // Switzerland
           { "CHL", "CL" },    // Chile
           { "CHN", "CN" },    // People's Republic of China
           { "COL", "CO" },    // Colombia
           { "CRI", "CR" },    // Costa Rica
           { "CZE", "CZ" },    // Czech Republic
           { "DEU", "DE" },    // Germany
           { "DNK", "DK" },    // Denmark
           { "DOM", "DO" },    // Dominican Republic
           { "DZA", "DZ" },    // Algeria
           { "ECU", "EC" },    // Ecuador
           { "EGY", "EG" },    // Egypt
           { "ESP", "ES" },    // Spain
           { "EST", "EE" },    // Estonia
           { "ETH", "ET" },    // Ethiopia
           { "FIN", "FI" },    // Finland
           { "FRA", "FR" },    // France
           { "FRO", "FO" },    // Faroe Islands
           { "GBR", "GB" },    // United Kingdom
           { "GEO", "GE" },    // Georgia
           { "GRC", "GR" },    // Greece
           { "GRL", "GL" },    // Greenland
           { "GTM", "GT" },    // Guatemala
           { "HKG", "HK" },    // Hong Kong S.A.R.
           { "HND", "HN" },    // Honduras
           { "HRV", "HR" },    // Croatia
           { "HUN", "HU" },    // Hungary
           { "IDN", "ID" },    // Indonesia
           { "IND", "IN" },    // India
           { "IRL", "IE" },    // Ireland
           { "IRN", "IR" },    // Iran
           { "IRQ", "IQ" },    // Iraq
           { "ISL", "IS" },    // Iceland
           { "ISR", "IL" },    // Israel
           { "ITA", "IT" },    // Italy
           { "JAM", "JM" },    // Jamaica
           { "JOR", "JO" },    // Jordan
           { "JPN", "JP" },    // Japan
           { "KAZ", "KZ" },    // Kazakhstan
           { "KEN", "KE" },    // Kenya
           { "KGZ", "KG" },    // Kyrgyzstan
           { "KHM", "KH" },    // Cambodia
           { "KOR", "KR" },    // Korea
           { "KWT", "KW" },    // Kuwait
           { "LAO", "LA" },    // Lao P.D.R.
           { "LBN", "LB" },    // Lebanon
           { "LBY", "LY" },    // Libya
           { "LIE", "LI" },    // Liechtenstein
           { "LKA", "LK" },    // Sri Lanka
           { "LTU", "LT" },    // Lithuania
           { "LUX", "LU" },    // Luxembourg
           { "LVA", "LV" },    // Latvia
           { "MAC", "MO" },    // Macao S.A.R.
           { "MAR", "MA" },    // Morocco
           { "MCO", "MC" },    // Principality of Monaco
           { "MDV", "MV" },    // Maldives
           { "MEX", "MX" },    // Mexico
           { "MKD", "MK" },    // Macedonia (FYROM)
           { "MLT", "MT" },    // Malta
           { "MNE", "ME" },    // Montenegro
           { "MNG", "MN" },    // Mongolia
           { "MYS", "MY" },    // Malaysia
           { "NGA", "NG" },    // Nigeria
           { "NIC", "NI" },    // Nicaragua
           { "NLD", "NL" },    // Netherlands
           { "NOR", "NO" },    // Norway
           { "NPL", "NP" },    // Nepal
           { "NZL", "NZ" },    // New Zealand
           { "OMN", "OM" },    // Oman
           { "PAK", "PK" },    // Islamic Republic of Pakistan
           { "PAN", "PA" },    // Panama
           { "PER", "PE" },    // Peru
           { "PHL", "PH" },    // Republic of the Philippines
           { "POL", "PL" },    // Poland
           { "PRI", "PR" },    // Puerto Rico
           { "PRT", "PT" },    // Portugal
           { "PRY", "PY" },    // Paraguay
           { "QAT", "QA" },    // Qatar
           { "ROU", "RO" },    // Romania
           { "RUS", "RU" },    // Russia
           { "RWA", "RW" },    // Rwanda
           { "SAU", "SA" },    // Saudi Arabia
           { "SCG", "CS" },    // Serbia and Montenegro (Former)
           { "SEN", "SN" },    // Senegal
           { "SGP", "SG" },    // Singapore
           { "SLV", "SV" },    // El Salvador
           { "SRB", "RS" },    // Serbia
           { "SVK", "SK" },    // Slovakia
           { "SVN", "SI" },    // Slovenia
           { "SWE", "SE" },    // Sweden
           { "SYR", "SY" },    // Syria
           { "TAJ", "TJ" },    // Tajikistan
           { "THA", "TH" },    // Thailand
           { "TKM", "TM" },    // Turkmenistan
           { "TTO", "TT" },    // Trinidad and Tobago
           { "TUN", "TN" },    // Tunisia
           { "TUR", "TR" },    // Turkey
           { "TWN", "TW" },    // Taiwan
           { "UKR", "UA" },    // Ukraine
           { "URY", "UY" },    // Uruguay
           { "USA", "US" },    // United States
           { "UZB", "UZ" },    // Uzbekistan
           { "VEN", "VE" },    // Bolivarian Republic of Venezuela
           { "VNM", "VN" },    // Vietnam
           { "YEM", "YE" },    // Yemen
           { "ZAF", "ZA" },    // South Africa
           { "ZWE", "ZW" },    // Zimbabwe
        };
    }
}
