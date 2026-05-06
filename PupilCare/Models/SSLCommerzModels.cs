using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PupilCare.Models
{
    public class SSLCommerzConfig
    {
        public string StoreId { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string InitUrl { get; set; } = string.Empty;
        public string ValidationUrl { get; set; } = string.Empty;
    }

    public class PaymentInitResponse
    {
        public string status { get; set; } = string.Empty;
        public string failedreason { get; set; } = string.Empty;
        public string sessionkey { get; set; } = string.Empty;
        [JsonProperty("GatewayPageURL")]
        public string GatewayPageURL { get; set; } = string.Empty;
    }

    public class PaymentValidationResponse
    {
        public string status { get; set; } = string.Empty;
        public string tran_id { get; set; } = string.Empty;
        public string val_id { get; set; } = string.Empty;
        public string amount { get; set; } = string.Empty;
        public string currency { get; set; } = string.Empty;
        public string bank_tran_id { get; set; } = string.Empty;
        public string card_type { get; set; } = string.Empty;
        public string card_no { get; set; } = string.Empty;
        public string card_issuer { get; set; } = string.Empty;
        public string card_brand { get; set; } = string.Empty;
        public string card_issuer_country { get; set; } = string.Empty;
        public string card_issuer_country_code { get; set; } = string.Empty;
        public string verify_sign { get; set; } = string.Empty;
        public string verify_key { get; set; } = string.Empty;
        public string APIConnect { get; set; } = string.Empty;
        public string validated_on { get; set; } = string.Empty;
        public string gw_version { get; set; } = string.Empty;
    }
}
