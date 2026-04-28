using System;
using System.Collections.Generic;

namespace PupilCare.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;           // e.g. "Monthly", "Yearly", "Trial"
        public decimal Price { get; set; }                          // In BDT
        public int DurationDays { get; set; }                      // 30, 365, or fraction (e.g. 0 for trial minutes)
        public int DurationMinutes { get; set; }                   // For short plans like 5-min trial
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
    }
}
