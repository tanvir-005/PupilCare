using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PupilCare.Data;
using PupilCare.Models;
using PupilCare.ViewModels;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleDashboard();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToRoleDashboard();
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleDashboard();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterSchoolAdminViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Create School
            var school = new School
            {
                Name = model.SchoolName,
                EIIN = model.EIIN,
                Address = model.SchoolAddress,
                IsApproved = false  // Pending super admin approval
            };
            _context.Schools.Add(school);
            await _context.SaveChangesAsync();

            // Create School Admin user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Designation = model.Designation ?? "School Administrator",
                SchoolId = school.Id,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "SchoolAdmin");
                TempData["SuccessMessage"] = "Registration successful! Your school is pending approval by the super administrator.";
                return RedirectToAction(nameof(Login));
            }

            // Rollback school if user creation fails
            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleDashboard()
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Dashboard", "SuperAdmin");
            if (User.IsInRole("SchoolAdmin"))
                return RedirectToAction("Dashboard", "SchoolAdmin");
            if (User.IsInRole("Teacher"))
                return RedirectToAction("Dashboard", "Teacher");
            return RedirectToAction("Index", "Home");
        }
    }
}
