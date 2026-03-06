using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PupilCare.Models;
using PupilCare.ViewModels;
using System.Threading.Tasks;
using PupilCare.Data;

namespace PupilCare.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterSchoolAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create the School first
                var school = new School
                {
                    Name = model.SchoolName,
                    Address = model.SchoolAddress,
                    Contact = model.SchoolContact,
                    IsApproved = false // Requires SuperAdmin approval
                };

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                // Create the User
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    SchoolId = school.Id
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "SchoolAdmin");
                    
                    // We don't sign them in immediately because their school is not approved yet
                    TempData["SuccessMessage"] = "Registration successful! You will be able to manage your school once a Super Admin approves it. Please log in to check your status.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
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
    }
}
