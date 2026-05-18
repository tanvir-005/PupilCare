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

        [DataType(DataType.MultilineText)]
        public string DefaultAiPrompt { get; set; } = "You are an educational analyst for PupilCare, a school management system in Bangladesh.\nYou have been given structured data about: {ScopeLabel}\nInsight Scope: {InsightScope}\n\nData:\n{Data}\n\nPlease provide a comprehensive analysis with the following sections:\n1. **Overall Summary** - Key statistics and general performance overview\n2. **Strengths** - Notable positives observed in the data\n3. **Areas of Concern** - Issues that need attention (poor attendance, low marks, behavioral patterns)\n4. **Actionable Recommendations** - Specific steps teachers or admin should take\n\nBe empathetic, professional, and constructive. Keep the response focused and practical.";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
