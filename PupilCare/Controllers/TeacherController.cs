using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Models;
using PupilCare.ViewModels;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        [HttpPost]
        public async Task<IActionResult> EditRecord(int id, string type, string text)
        {
            var record = await _context.Records.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (record != null && record.TeacherId == user.Id)
            {
                record.Type = type;
                record.Text = text;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Record updated.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var record = await _context.Records.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (record != null && record.TeacherId == user.Id)
            {
                _context.Records.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Record deleted.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> ExportClassJson(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();

            var classroom = await _context.Classrooms
                .Include(c => c.Students)
                    .ThenInclude(s => s.Records)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.SchoolId == user.SchoolId);

            if (classroom == null) return NotFound();

            var exportData = new
            {
                ClassName = classroom.Name,
                GradeLevel = classroom.GradeLevel,
                Students = classroom.Students.Select(s => new
                {
                    StudentId = s.StudentId,
                    Name = s.Name,
                    Gender = s.Gender,
                    DateOfBirth = s.DateOfBirth.ToString("yyyy-MM-dd"),
                    Address = s.Address,
                    Contact = s.Contact,
                    Records = s.Records.Select(r => new
                    {
                        Type = r.Type,
                        Text = r.Text,
                        CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList()
                }).ToList()
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(exportData, options);

            return File(Encoding.UTF8.GetBytes(jsonString), "application/json", $"Class_{classroom.Name.Replace(" ", "_")}_Export.json");
        }

        [HttpGet]
        public async Task<IActionResult> ExportStudentJson(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.Classroom)
                .Include(s => s.Records)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.Classroom.SchoolId == user.SchoolId);

            if (student == null) return NotFound();

            var exportData = new
            {
                StudentId = student.StudentId,
                Name = student.Name,
                ClassName = student.Classroom.Name,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth.ToString("yyyy-MM-dd"),
                Address = student.Address,
                Contact = student.Contact,
                Records = student.Records.Select(r => new
                {
                    Type = r.Type,
                    Text = r.Text,
                    CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList()
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(exportData, options);

            return File(Encoding.UTF8.GetBytes(jsonString), "application/json", $"Student_{student.StudentId}_Export.json");
        }
    }
}
