using Microsoft.AspNetCore.Mvc;
using PupilCare.Models;
using System.Diagnostics;

namespace PupilCare.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About() => View();
        public IActionResult Services() => View();
        public IActionResult Career() => View();
        public IActionResult Partners() => View();
        public IActionResult PrivacyPolicy() => View();
        public IActionResult TermsOfService() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
