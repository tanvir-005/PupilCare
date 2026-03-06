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
    [Authorize(Roles = "SchoolAdmin")]
    public class SchoolAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchoolAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return RedirectToAction("Login", "Auth");

            var school = await _context.Schools
                .Include(s => s.Classrooms)
                .ThenInclude(c => c.Students)
                .FirstOrDefaultAsync(s => s.Id == user.SchoolId);

            if (school == null) return NotFound();

            if (!school.IsApproved)
            {
                return View("NotApproved", school);
            }

            var teachers = await _userManager.Users.Where(u => u.SchoolId == school.Id).ToListAsync();
            var teacherRoleUsers = await _userManager.GetUsersInRoleAsync("Teacher");
            
            ViewBag.Teachers = teachers.Intersect(teacherRoleUsers).ToList();

            return View(school);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClassroom(CreateClassroomViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid && user?.SchoolId != null)
            {
                var classroom = new Classroom
                {
                    Name = model.Name,
                    GradeLevel = model.GradeLevel,
                    SchoolId = user.SchoolId.Value
                };
                _context.Classrooms.Add(classroom);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Classroom created successfully.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid && user?.SchoolId != null)
            {
                var teacher = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    SchoolId = user.SchoolId.Value
                };

                var result = await _userManager.CreateAsync(teacher, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(teacher, "Teacher");
                    TempData["SuccessMessage"] = "Teacher account created successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> EnrollStudent(EnrollStudentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (ModelState.IsValid && user?.SchoolId != null)
            {
                // Verify classroom belongs to this school
                var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == model.ClassroomId && c.SchoolId == user.SchoolId.Value);
                if (classroom != null)
                {
                    var student = new Student
                    {
                        Name = model.Name,
                        Address = model.Address,
                        Contact = model.Contact,
                        Gender = model.Gender,
                        DateOfBirth = model.DateOfBirth,
                        ClassroomId = classroom.Id
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Student enrolled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid classroom.";
                }
            }
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> ClassroomOverview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return RedirectToAction("Login", "Auth");

            var classroom = await _context.Classrooms
                .Include(c => c.Students)
                .ThenInclude(s => s.Records)
                .FirstOrDefaultAsync(c => c.Id == id && c.SchoolId == user.SchoolId.Value);

            if (classroom == null) return NotFound();

            return View(classroom);
        }
    }
}
