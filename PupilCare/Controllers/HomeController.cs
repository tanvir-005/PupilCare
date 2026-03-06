using Microsoft.AspNetCore.Mvc;
using PupilCare.Models;
using System.Diagnostics;

namespace PupilCare.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("SuperAdmin"))
                    return RedirectToAction("Dashboard", "SuperAdmin");
                if (User.IsInRole("SchoolAdmin"))
                    return RedirectToAction("Dashboard", "SchoolAdmin");
                if (User.IsInRole("Teacher"))
                    return RedirectToAction("Dashboard", "Teacher");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
