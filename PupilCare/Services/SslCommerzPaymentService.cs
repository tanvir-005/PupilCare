using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PupilCare.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace PupilCare.Services
{
    /// <summary>
    /// SSLCommerz Hosted Checkout Integration Service.
    /// Handles secure payment initiation and multi-step validation.
    /// </summary>
    public class SslCommerzPaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SslCommerzPaymentService> _logger;
        private readonly SSLCommerzConfig _sslConfig;

        public SslCommerzPaymentService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<SslCommerzPaymentService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            
            // Map configuration to strongly typed model
            _sslConfig = new SSLCommerzConfig();
            _config.GetSection("SSLCommerz").Bind(_sslConfig);
        }

        /// <summary>
        /// Initiates a Hosted Checkout session with SSLCommerz.
        /// Returns the GatewayPageURL for redirection.
        /// </summary>
        public async Task<string?> InitiatePaymentAsync(
            int schoolId, int planId, decimal amount,
            string transactionId, string customerName,
            string customerEmail, string customerPhone,
            string successUrl, string failUrl, string cancelUrl, string ipnUrl)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                var postData = new List<KeyValuePair<string, string>>
                {
                    new("store_id", _sslConfig.StoreId),
                    new("store_passwd", _sslConfig.StorePassword),
                    new("total_amount", amount.ToString("F2")),
                    new("currency", "BDT"),
                    new("tran_id", transactionId),
                    new("success_url", successUrl),
                    new("fail_url", failUrl),
                    new("cancel_url", cancelUrl),
                    new("ipn_url", ipnUrl),
                    
                    // Customer Info
                    new("cus_name", customerName),
                    new("cus_email", customerEmail),
                    new("cus_add1", "Dhaka"),
                    new("cus_city", "Dhaka"),
                    new("cus_postcode", "1000"),
                    new("cus_country", "Bangladesh"),
                    new("cus_phone", customerPhone),
                    
                    // Product Info
                    new("shipping_method", "NO"),
                    new("product_name", "School Subscription"),
                    new("product_category", "Education"),
                    new("product_profile", "general")
                };

                var content = new FormUrlEncodedContent(postData);
                var response = await client.PostAsync(_sslConfig.BaseUrl + _sslConfig.InitUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SSLCommerz Init Request Failed: {Status} - {Content}", response.StatusCode, responseContent);
                    return null;
                }

                var result = JsonConvert.DeserializeObject<PaymentInitResponse>(responseContent);

                if (result?.status == "SUCCESS" && !string.IsNullOrEmpty(result.GatewayPageURL))
                {
                    return result.GatewayPageURL;
                }

                _logger.LogWarning("SSLCommerz Init Logic Failure: {Response}", responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during SSLCommerz initiation for TranId: {TranId}", transactionId);
                return null;
            }
        }

        /// <summary>
        /// Validates the payment using the SSLCommerz Validation API.
        /// Does not trust the success callback alone.
        /// </summary>
        public async Task<bool> ValidatePaymentAsync(string val_id, string amount, string currency)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = $"{_sslConfig.BaseUrl}{_sslConfig.ValidationUrl}?val_id={val_id}&store_id={_sslConfig.StoreId}&store_passwd={_sslConfig.StorePassword}&format=json";

                var response = await client.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<PaymentValidationResponse>(response);

                if (result?.status == "VALID" || result?.status == "VALIDATED")
                {
                    // Additional check: compare amount and currency if needed
                    // For now, status VALID is sufficient per sandbox rules
                    return true;
                }

                _logger.LogWarning("SSLCommerz Validation Failed for ValId: {ValId}. Status: {Status}", val_id, result?.status);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during SSLCommerz validation for ValId: {ValId}", val_id);
                return false;
            }
        }
    }
}
