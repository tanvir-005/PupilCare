using System.Threading.Tasks;

namespace PupilCare.Services
{
    public interface IPaymentService
    {
        Task<string?> InitiatePaymentAsync(int schoolId, int planId, decimal amount, string transactionId, string customerName, string customerEmail, string customerPhone);
        Task<bool> ValidatePaymentAsync(string transactionId, string amount, string currency);
    }
}
