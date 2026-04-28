using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class School
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? EIIN { get; set; }

        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }

        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// Null = never purchased a subscription.
        /// Past date = subscription expired.
        /// Future date = subscription active.
        /// </summary>
        public DateTime? SubscriptionExpiry { get; set; }

        public ICollection<ClassLevel> ClassLevels { get; set; } = new List<ClassLevel>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
        public ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new List<SubscriptionPayment>();
        public ICollection<AiInsight> AiInsights { get; set; } = new List<AiInsight>();
    }
}
