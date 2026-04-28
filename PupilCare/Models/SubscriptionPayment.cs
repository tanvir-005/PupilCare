using System;

namespace PupilCare.Models
{
    /// <summary>
    /// Tracks each SSLCommerz payment attempt for school subscription renewal.
    /// </summary>
    public class SubscriptionPayment
    {
        public int Id { get; set; }

        public int SchoolId { get; set; }
        public School School { get; set; } = null!;

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan Plan { get; set; } = null!;

        public decimal Amount { get; set; }              // Amount in BDT at time of payment
        public string TransactionId { get; set; } = string.Empty;  // PupilCare-generated tran_id
        public string? GatewayTransactionId { get; set; }          // SSLCommerz bank_tran_id

        /// <summary>Pending | Success | Failed | Cancelled</summary>
        public string Status { get; set; } = "Pending";

        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? NewExpiryDate { get; set; }   // Set on success

        public string? SslCommerzValidationResponse { get; set; }  // Raw JSON for audit
    }
}
