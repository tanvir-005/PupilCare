using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Models;
using PupilCare.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuperAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var schools = await _context.Schools
                .Include(s => s.Users)
                .Include(s => s.ClassLevels)
                    .ThenInclude(cl => cl.Sections)
                        .ThenInclude(sec => sec.Students)
                .Include(s => s.SubscriptionPayments)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            ViewBag.TotalSchools = schools.Count;
            ViewBag.ApprovedSchools = schools.Count(s => s.IsApproved);
            ViewBag.PendingSchools = schools.Count(s => !s.IsApproved);
            ViewBag.ActiveSubscriptions = schools.Count(s => s.SubscriptionExpiry.HasValue && s.SubscriptionExpiry > DateTime.UtcNow);

            ViewData["ActiveTab"] = "Schools";
            return View(schools);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSchool(int id)
        {
            var school = await _context.Schools.FindAsync(id);
            if (school != null)
            {
                school.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"\"{school.Name}\" has been approved.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSchool(int id)
        {
            var school = await _context.Schools.FindAsync(id);
            if (school != null)
            {
                school.IsApproved = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"\"{school.Name}\" approval has been revoked.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> SchoolDetails(int id)
        {
            var school = await _context.Schools
                .Include(s => s.ClassLevels)
                    .ThenInclude(cl => cl.Sections)
                        .ThenInclude(sec => sec.Students)
                .Include(s => s.ClassLevels)
                    .ThenInclude(cl => cl.Subjects)
                .Include(s => s.Users)
                .Include(s => s.SubscriptionPayments)
                    .ThenInclude(p => p.Plan)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (school == null) return NotFound();

            var adminUsers = new System.Collections.Generic.List<ApplicationUser>();
            var teacherUsers = new System.Collections.Generic.List<ApplicationUser>();

            foreach (var user in school.Users)
            {
                if (await _userManager.IsInRoleAsync(user, "SchoolAdmin"))
                    adminUsers.Add(user);
                else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                    teacherUsers.Add(user);
            }

            ViewBag.AdminUsers = adminUsers;
            ViewBag.TeacherUsers = teacherUsers;

            return View(school);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchool(int id)
        {
            var school = await _context.Schools
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (school != null)
            {
                // Delete associated users first
                foreach (var user in school.Users.ToList())
                    await _userManager.DeleteAsync(user);

                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "School deleted successfully.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // ── Subscription Plans ────────────────────────────────────────────────
        public async Task<IActionResult> SubscriptionPlans()
        {
            var plans = await _context.SubscriptionPlans
                .Include(p => p.Payments)
                .OrderBy(p => p.Price)
                .ToListAsync();
            ViewData["ActiveTab"] = "Plans";
            return View(plans);
        }

        [HttpGet]
        public IActionResult CreatePlan()
        {
            return View(new CreateSubscriptionPlanViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(CreateSubscriptionPlanViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            _context.SubscriptionPlans.Add(new SubscriptionPlan
            {
                Name = model.Name,
                Price = model.Price,
                DurationDays = model.DurationDays,
                DurationMinutes = model.DurationMinutes,
                Description = model.Description,
                IsActive = model.IsActive
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Plan \"{model.Name}\" created.";
            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpGet]
        public async Task<IActionResult> EditPlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound();

            return View(new EditSubscriptionPlanViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                DurationMinutes = plan.DurationMinutes,
                Description = plan.Description,
                IsActive = plan.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(EditSubscriptionPlanViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var plan = await _context.SubscriptionPlans.FindAsync(model.Id);
            if (plan == null) return NotFound();

            plan.Name = model.Name;
            plan.Price = model.Price;
            plan.DurationDays = model.DurationDays;
            plan.DurationMinutes = model.DurationMinutes;
            plan.Description = model.Description;
            plan.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Plan \"{plan.Name}\" updated.";
            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Plan deleted.";
            }
            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantSubscription(int schoolId, int months)
        {
            var school = await _context.Schools.FindAsync(schoolId);
            if (school == null) return NotFound();

            var current = school.SubscriptionExpiry.HasValue && school.SubscriptionExpiry > DateTime.UtcNow
                ? school.SubscriptionExpiry.Value
                : DateTime.UtcNow;

            school.SubscriptionExpiry = current.AddMonths(months);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Subscription extended for \"{school.Name}\" until {school.SubscriptionExpiry:dd MMM yyyy}.";
            return RedirectToAction(nameof(SchoolDetails), new { id = schoolId });
        }

        // ── System Settings ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSetting();
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            ViewData["ActiveTab"] = "Settings";
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SystemSetting model)
        {
            if (!ModelState.IsValid) return View(model);

            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) return NotFound();

            settings.SystemName = model.SystemName;
            settings.ContactEmail = model.ContactEmail;
            settings.ContactPhone = model.ContactPhone;
            settings.Address = model.Address;
            settings.AboutUs = model.AboutUs;
            settings.Careers = model.Careers;
            settings.Partners = model.Partners;
            settings.PrivacyPolicy = model.PrivacyPolicy;
            settings.TermsAndConditions = model.TermsAndConditions;
            settings.CertificationInfo = model.CertificationInfo;
            settings.DefaultAiPrompt = model.DefaultAiPrompt;
            settings.FacebookUrl = model.FacebookUrl;
            settings.TwitterUrl = model.TwitterUrl;
            settings.LinkedInUrl = model.LinkedInUrl;
            settings.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "System settings updated successfully.";
            return RedirectToAction(nameof(Settings));
        }

        // ── Contact Messages ─────────────────────────────────────────────────
        public async Task<IActionResult> Messages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
            ViewData["ActiveTab"] = "Messages";
            return View(messages);
        }

        public async Task<IActionResult> MessageDetails(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyMessage(int id, string reply)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.ReplyMessage = reply;
            message.RepliedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reply saved.";
            return RedirectToAction(nameof(MessageDetails), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message deleted.";
            }
            return RedirectToAction(nameof(Messages));
        }

        // ── User Management ──────────────────────────────────────────────────
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var userRoles = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = string.Join(", ", roles);
            }

            ViewBag.UserRoles = userRoles;
            ViewData["ActiveTab"] = "Users";
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {user.Email} is now {(user.IsActive ? "Active" : "Inactive")}.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string id, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password for {user.Email} has been reset.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Users));
        }

        // ── System Logs ──────────────────────────────────────────────────────
        public async Task<IActionResult> Logs()
        {
            // Fetching recent critical data as a form of "logs"
            var recentPayments = await _context.SubscriptionPayments
                .Include(p => p.School)
                .OrderByDescending(p => p.InitiatedAt)
                .Take(10)
                .ToListAsync();

            var recentSchools = await _context.Schools
                .OrderByDescending(s => s.Id)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentPayments = recentPayments;
            ViewBag.RecentSchools = recentSchools;

            ViewData["ActiveTab"] = "Logs";
            return View();
        }
    }
}
