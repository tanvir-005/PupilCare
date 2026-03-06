using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var schools = await _context.Schools.ToListAsync();
            return View(schools);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveSchool(int id)
        {
            var school = await _context.Schools.FindAsync(id);
            if (school != null)
            {
                school.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"School '{school.Name}' has been approved.";
            }
            return RedirectToAction(nameof(Dashboard));
        }
        
        [HttpPost]
        public async Task<IActionResult> RejectSchool(int id)
        {
            // For MVP, rejecting might mean deleting or leaving unapproved. We'll just delete it for simplicity.
            var school = await _context.Schools.FindAsync(id);
            if (school != null)
            {
                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"School '{school.Name}' registration rejected and removed.";
            }
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
