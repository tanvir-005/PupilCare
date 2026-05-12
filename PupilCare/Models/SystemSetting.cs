using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class SystemSetting
    {
        public int Id { get; set; }

        [Required]
        public string SystemName { get; set; } = "PupilCare";

        public string ContactEmail { get; set; } = "support@pupilcare.com";
        public string ContactPhone { get; set; } = "+1234567890";
        public string Address { get; set; } = "123 Education Lane, Learning City";

        [DataType(DataType.MultilineText)]
        public string AboutUs { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Careers { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Partners { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string PrivacyPolicy { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string TermsAndConditions { get; set; } = string.Empty;

        public string FacebookUrl { get; set; } = string.Empty;
        public string TwitterUrl { get; set; } = string.Empty;
        public string LinkedInUrl { get; set; } = string.Empty;
        
        public string CertificationInfo { get; set; } = string.Empty;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
