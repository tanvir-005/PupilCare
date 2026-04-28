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
    }
}
