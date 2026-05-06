using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Models;
using PupilCare.Services;
using System;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    [AllowAnonymous] // Callbacks come from SSLCommerz servers
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Success([FromForm] string tran_id, [FromForm] string val_id, [FromForm] string amount, [FromForm] string currency)
        {
            _logger.LogInformation("Payment Success Callback received for TranId: {TranId}, ValId: {ValId}", tran_id, val_id);
            return await ProcessPayment(tran_id, val_id, amount, currency, "Success");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Fail([FromForm] string tran_id)
        {
            _logger.LogWarning("Payment Fail Callback received for TranId: {TranId}", tran_id);
            var payment = await _context.SubscriptionPayments.FirstOrDefaultAsync(p => p.TransactionId == tran_id);
            if (payment != null)
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Subscription", "SchoolAdmin");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Cancel([FromForm] string tran_id)
        {
            _logger.LogWarning("Payment Cancel Callback received for TranId: {TranId}", tran_id);
            var payment = await _context.SubscriptionPayments.FirstOrDefaultAsync(p => p.TransactionId == tran_id);
            if (payment != null)
            {
                payment.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Subscription", "SchoolAdmin");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IPN([FromForm] string tran_id, [FromForm] string val_id, [FromForm] string amount, [FromForm] string currency, [FromForm] string status)
        {
            _logger.LogInformation("IPN Received for TranId: {TranId}, Status: {Status}", tran_id, status);
            
            if (status == "VALID" || status == "VALIDATED")
            {
                await ProcessPayment(tran_id, val_id, amount, currency, "Success");
            }
            else
            {
                var payment = await _context.SubscriptionPayments.FirstOrDefaultAsync(p => p.TransactionId == tran_id);
                if (payment != null && payment.Status == "Pending")
                {
                    payment.Status = status;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(); // Always return OK to SSLCommerz for IPN
        }

        private async Task<IActionResult> ProcessPayment(string tran_id, string val_id, string amount, string currency, string finalStatus)
        {
            var payment = await _context.SubscriptionPayments
                .Include(p => p.School)
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.TransactionId == tran_id);

            if (payment == null) return NotFound();
            
            // If already processed (e.g. by IPN), just redirect
            if (payment.Status == "Success") return RedirectToAction("Subscription", "SchoolAdmin");

            // Validate with SSLCommerz API (Requirement #9)
            var isValid = await _paymentService.ValidatePaymentAsync(val_id, amount, currency);

            if (isValid)
            {
                payment.Status = "Success";
                payment.CompletedAt = DateTime.UtcNow;
                payment.GatewayTransactionId = val_id;

                var school = payment.School;
                var plan = payment.Plan;

                var baseDate = (school.SubscriptionExpiry.HasValue && school.SubscriptionExpiry.Value > DateTime.UtcNow) 
                    ? school.SubscriptionExpiry.Value 
                    : DateTime.UtcNow;
                
                school.SubscriptionExpiry = baseDate.AddDays(plan.DurationDays).AddMinutes(plan.DurationMinutes);
                payment.NewExpiryDate = school.SubscriptionExpiry;

                await _context.SaveChangesAsync();
            }
            else
            {
                payment.Status = "ValidationFailed";
                await _context.SaveChangesAsync();
            }

            // Redirect back to subscription page where status will be shown
            return RedirectToAction("Subscription", "SchoolAdmin");
        }
    }
}
