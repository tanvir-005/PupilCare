using System.Threading.Tasks;

namespace PupilCare.Services
{
    public interface IAiInsightService
    {
        Task<string> GenerateInsightAsync(string insightScope, string scopeLabel, object structuredData);
    }
}
