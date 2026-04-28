using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PupilCare.Services
{
    /// <summary>
    /// SSLCommerz payment gateway integration.
    /// Handles payment initiation and validation for subscription renewals.
    /// </summary>
    public class SslCommerzPaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SslCommerzPaymentService> _logger;

        public SslCommerzPaymentService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<SslCommerzPaymentService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Initiates an SSLCommerz payment session.
        /// Returns the GatewayPageURL to redirect the user to, or null on failure.
        /// </summary>
        public async Task<string?> InitiatePaymentAsync(
            int schoolId, int planId, decimal amount,
            string transactionId, string customerName,
            string customerEmail, string customerPhone)
        {
            try
            {
                var ssl = _config.GetSection("SSLCommerz");
                var storeId = ssl["StoreId"] ?? "testbox";
                var storePass = ssl["StorePassword"] ?? "qwerty";
                var baseUrl = ssl["BaseUrl"] ?? "https://sandbox.sslcommerz.com";
                var initUrl = ssl["InitUrl"] ?? "/gwprocess/v4/api.php";
                var successUrl = ssl["SuccessUrl"] ?? "https://localhost/SchoolAdmin/PaymentSuccess";
                var failUrl = ssl["FailUrl"] ?? "https://localhost/SchoolAdmin/PaymentFail";
                var cancelUrl = ssl["CancelUrl"] ?? "https://localhost/SchoolAdmin/PaymentCancel";

                var formData = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("store_id", storeId),
                    new System.Collections.Generic.KeyValuePair<string, string>("store_passwd", storePass),
                    new System.Collections.Generic.KeyValuePair<string, string>("total_amount", amount.ToString("F2")),
                    new System.Collections.Generic.KeyValuePair<string, string>("currency", "BDT"),
                    new System.Collections.Generic.KeyValuePair<string, string>("tran_id", transactionId),
                    new System.Collections.Generic.KeyValuePair<string, string>("success_url", successUrl),
                    new System.Collections.Generic.KeyValuePair<string, string>("fail_url", failUrl),
                    new System.Collections.Generic.KeyValuePair<string, string>("cancel_url", cancelUrl),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_name", customerName),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_email", customerEmail),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_phone", customerPhone),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_add1", "Bangladesh"),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_city", "Dhaka"),
                    new System.Collections.Generic.KeyValuePair<string, string>("cus_country", "Bangladesh"),
                    new System.Collections.Generic.KeyValuePair<string, string>("shipping_method", "NO"),
                    new System.Collections.Generic.KeyValuePair<string, string>("product_name", "PupilCare Subscription"),
                    new System.Collections.Generic.KeyValuePair<string, string>("product_category", "Software"),
                    new System.Collections.Generic.KeyValuePair<string, string>("product_profile", "general"),
                    new System.Collections.Generic.KeyValuePair<string, string>("product_amount", amount.ToString("F2")),
                });

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(baseUrl + initUrl, formData);
                var responseBody = await response.Content.ReadAsStringAsync();

                var json = JsonDocument.Parse(responseBody);
                if (json.RootElement.TryGetProperty("GatewayPageURL", out var gatewayUrl))
                    return gatewayUrl.GetString();

                _logger.LogWarning("SSLCommerz initiation failed: {Response}", responseBody);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating SSLCommerz payment");
                return null;
            }
        }

        /// <summary>
        /// Validates an SSLCommerz IPN callback using the validation API.
        /// </summary>
        public async Task<bool> ValidatePaymentAsync(string transactionId, string amount, string currency)
        {
            try
            {
                var ssl = _config.GetSection("SSLCommerz");
                var storeId = ssl["StoreId"] ?? "testbox";
                var storePass = ssl["StorePassword"] ?? "qwerty";
                var baseUrl = ssl["BaseUrl"] ?? "https://sandbox.sslcommerz.com";
                var validationUrl = ssl["ValidationUrl"] ?? "/validator/api/validationserverAPI.php";

                var url = $"{baseUrl}{validationUrl}?val_id={transactionId}&store_id={storeId}&store_passwd={storePass}&format=json";
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                if (json.RootElement.TryGetProperty("status", out var status))
                    return status.GetString() == "VALID" || status.GetString() == "VALIDATED";

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SSLCommerz payment");
                return false;
            }
        }
    }
}
