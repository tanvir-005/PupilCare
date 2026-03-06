using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Models;
using PupilCare.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return RedirectToAction("Login", "Auth");

            // Teacher only sees classrooms in their school
            var classrooms = await _context.Classrooms
                .Include(c => c.Students)
                .ThenInclude(s => s.Records)
                .Where(c => c.SchoolId == user.SchoolId)
                .ToListAsync();

            return View(classrooms);
        }

        [HttpPost]
        public async Task<IActionResult> AddRecord(AddRecordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid && user != null)
            {
                var record = new Record
                {
                    StudentId = model.StudentId,
                    TeacherId = user.Id,
                    Type = model.Type,
                    Text = model.Text
                };
                
                _context.Records.Add(record);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Record added successfully.";
            }
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
