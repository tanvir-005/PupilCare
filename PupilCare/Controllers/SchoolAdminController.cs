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
            
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Dashboard));
            }

            if (user?.SchoolId == null)
            {
                TempData["ErrorMessage"] = "User or school information not found.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Verify classroom belongs to this school
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == model.ClassroomId && c.SchoolId == user.SchoolId.Value);
            if (classroom == null)
            {
                TempData["ErrorMessage"] = "Invalid classroom or classroom does not belong to your school.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Check for duplicate StudentId in the school
            var exists = await _context.Students.Include(s => s.Classroom)
                .AnyAsync(s => s.StudentId == model.StudentId && s.Classroom.SchoolId == user.SchoolId.Value);
            if (exists)
            {
                TempData["ErrorMessage"] = "A student with this ID already exists in your school.";
                return RedirectToAction(nameof(Dashboard));
            }

            var student = new Student
            {
                StudentId = model.StudentId,
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

        [HttpPost]
        public async Task<IActionResult> EditClassroom(int id, string name, string gradeLevel)
        {
            var user = await _userManager.GetUserAsync(User);
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id && c.SchoolId == user.SchoolId);
            if (classroom != null)
            {
                classroom.Name = name;
                classroom.GradeLevel = gradeLevel;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Classroom updated.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClassroom(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var classroom = await _context.Classrooms.FirstOrDefaultAsync(c => c.Id == id && c.SchoolId == user.SchoolId);
            if (classroom != null)
            {
                _context.Classrooms.Remove(classroom);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Classroom deleted.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);
            var user = await _userManager.GetUserAsync(User);
            if (teacher != null && teacher.SchoolId == user.SchoolId)
            {
                await _userManager.DeleteAsync(teacher);
                TempData["SuccessMessage"] = "Teacher deleted.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> EditStudent(int id, string name, string gender, string address, string contact)
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.Include(s => s.Classroom).FirstOrDefaultAsync(s => s.Id == id && s.Classroom.SchoolId == user.SchoolId);
            if (student != null)
            {
                student.Name = name;
                student.Gender = gender;
                student.Address = address;
                student.Contact = contact;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student updated.";
            }
            return RedirectToAction("ClassroomOverview", new { id = student?.ClassroomId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var student = await _context.Students.Include(s => s.Classroom).FirstOrDefaultAsync(s => s.Id == id && s.Classroom.SchoolId == user.SchoolId);
            int? classroomId = student?.ClassroomId;
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Student deleted.";
            }
            return RedirectToAction("ClassroomOverview", new { id = classroomId });
        }

        [HttpPost]
        public async Task<IActionResult> AddRecord(AddRecordViewModel model, string returnUrl = null)
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
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> EditRecord(int id, string type, string text, string returnUrl = null)
        {
            var record = await _context.Records.FindAsync(id);
            if (record != null)
            {
                record.Type = type;
                record.Text = text;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Record updated.";
            }
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRecord(int id, string returnUrl = null)
        {
            var record = await _context.Records.FindAsync(id);
            if (record != null)
            {
                _context.Records.Remove(record);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Record deleted.";
            }
            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> ExportRecords()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();

            var records = await _context.Records
                .Include(r => r.Student)
                .ThenInclude(s => s.Classroom)
                .Where(r => r.Student.Classroom.SchoolId == user.SchoolId)
                .Select(r => new
                {
                    StudentId = r.Student.StudentId,
                    StudentName = r.Student.Name,
                    ClassroomName = r.Student.Classroom.Name,
                    RecordType = r.Type,
                    Date = r.CreatedAt.ToString("yyyy-MM-dd"),
                    Description = r.Text
                })
                .ToListAsync();

            return Json(records);
        }
    }
}
