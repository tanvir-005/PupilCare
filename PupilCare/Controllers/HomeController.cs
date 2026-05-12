using Microsoft.AspNetCore.Mvc;
using PupilCare.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    public class HomeController : Controller
    {
        private readonly Data.ApplicationDbContext _context;

        public HomeController(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> About()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        public async Task<IActionResult> Services()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        public async Task<IActionResult> Career()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        public async Task<IActionResult> Partners()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        public async Task<IActionResult> PrivacyPolicy()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        public async Task<IActionResult> TermsOfService()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(string email, string message)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(message))
            {
                TempData["ErrorMessage"] = "Email and message are required.";
                return RedirectToAction("Index");
            }

            var contactMsg = new ContactMessage
            {
                Email = email,
                Message = message,
                Name = email.Split('@')[0], // Default name from email
                Subject = "Inquiry from Website Footer",
                SentAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your message has been sent. We will contact you soon!";
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
