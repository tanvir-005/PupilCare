using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Filters;
using PupilCare.Models;
using PupilCare.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using PupilCare.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "SchoolAdmin")]
    [WriteGuard]
    public class SchoolAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymentService _paymentService;

        public SchoolAdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IPaymentService paymentService)
        {
            _context = context;
            _userManager = userManager;
            _paymentService = paymentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return RedirectToAction("Login", "Auth");

            var school = await _context.Schools
                .Include(s => s.ClassLevels)
                    .ThenInclude(cl => cl.Sections)
                        .ThenInclude(sec => sec.Students)
                .Include(s => s.Users)
                .FirstOrDefaultAsync(s => s.Id == user.SchoolId);

            if (school == null) return NotFound();

            if (!school.IsApproved)
            {
                return View("NotApproved", school);
            }

            ViewBag.School = school;
            ViewBag.Teachers = await GetSchoolTeachersAsync(user.SchoolId.Value);
            ViewBag.Students = school.ClassLevels.SelectMany(c => c.Sections).SelectMany(s => s.Students).Count();
            return View(school);
        }

        public async Task<IActionResult> Classes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();

            var classLevels = await _context.ClassLevels
                .Include(cl => cl.Sections)
                .Include(cl => cl.Subjects)
                .Where(cl => cl.SchoolId == user.SchoolId)
                .OrderBy(cl => cl.Order)
                .ToListAsync();

            return View(classLevels);
        }

        [HttpGet]
        public IActionResult CreateClass() => View(new CreateClassLevelViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(CreateClassLevelViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!ModelState.IsValid) return View(model);

            _context.ClassLevels.Add(new ClassLevel { Name = model.Name, Order = model.Order, SchoolId = user.SchoolId.Value });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Class created.";
            return RedirectToAction(nameof(Classes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSection(CreateSectionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var classLevel = await _context.ClassLevels.FirstOrDefaultAsync(c => c.Id == model.ClassLevelId && c.SchoolId == user.SchoolId);
            if (classLevel == null) return NotFound();
            if (!ModelState.IsValid) return RedirectToAction(nameof(Classes));

            _context.ClassSections.Add(new ClassSection { Name = model.Name, ClassLevelId = classLevel.Id });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Classes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(CreateSubjectViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var classLevel = await _context.ClassLevels.FirstOrDefaultAsync(c => c.Id == model.ClassLevelId && c.SchoolId == user.SchoolId);
            if (classLevel == null) return NotFound();
            if (!ModelState.IsValid) return RedirectToAction(nameof(Classes));

            _context.Subjects.Add(new Subject { Name = model.Name, ClassLevelId = classLevel.Id });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Classes));
        }

        public async Task<IActionResult> Teachers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();

            var teachers = await GetSchoolTeachersAsync(user.SchoolId.Value);
            var assignments = await _context.TeacherAssignments
                .Include(a => a.Teacher)
                .Include(a => a.ClassSection).ThenInclude(s => s.ClassLevel)
                .Include(a => a.Subject)
                .Where(a => a.Teacher.SchoolId == user.SchoolId)
                .OrderBy(a => a.Teacher.FullName)
                .ToListAsync();

            ViewBag.Assignments = assignments;
            ViewBag.Classes = await _context.ClassLevels
                .Include(c => c.Sections)
                .Include(c => c.Subjects)
                .Where(c => c.SchoolId == user.SchoolId)
                .OrderBy(c => c.Order)
                .ToListAsync();
            return View(teachers);
        }

        [HttpGet]
        public IActionResult CreateTeacher() => View(new CreateTeacherViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
        {
            var current = await _userManager.GetUserAsync(User);
            if (current?.SchoolId == null) return Unauthorized();
            if (!ModelState.IsValid) return View(model);

            var teacher = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.Phone,
                Phone = model.Phone,
                Designation = model.Designation,
                SchoolId = current.SchoolId,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(teacher, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(teacher, "Teacher");
                return RedirectToAction(nameof(Teachers));
            }

            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacher(AssignTeacherViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!ModelState.IsValid) return RedirectToAction(nameof(Teachers));

            var teacher = await _userManager.FindByIdAsync(model.TeacherId);
            var section = await _context.ClassSections.Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.ClassSectionId && s.ClassLevel.SchoolId == user.SchoolId);
            if (section == null) return BadRequest();
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId && s.ClassLevelId == section.ClassLevelId);

            if (teacher?.SchoolId != user.SchoolId || subject == null) return BadRequest();
            if (await _context.TeacherAssignments.AnyAsync(a => a.ClassSectionId == model.ClassSectionId && a.SubjectId == model.SubjectId))
            {
                TempData["ErrorMessage"] = "That section-subject already has a teacher.";
                return RedirectToAction(nameof(Teachers));
            }

            _context.TeacherAssignments.Add(new TeacherAssignment
            {
                TeacherId = model.TeacherId,
                ClassSectionId = model.ClassSectionId,
                SubjectId = model.SubjectId
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Teachers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetClassTeacher(SetClassTeacherViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var classLevel = await _context.ClassLevels.FirstOrDefaultAsync(c => c.Id == model.ClassLevelId && c.SchoolId == user.SchoolId);
            if (classLevel == null) return NotFound();

            if (!string.IsNullOrEmpty(model.TeacherId))
            {
                var teacher = await _userManager.FindByIdAsync(model.TeacherId);
                if (teacher?.SchoolId != user.SchoolId || !await _userManager.IsInRoleAsync(teacher, "Teacher")) return BadRequest();
            }

            classLevel.ClassTeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Teachers));
        }

        public async Task<IActionResult> Students(int? classLevelId, int? sectionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            await LoadSchoolStructureAsync(user.SchoolId.Value);

            var query = _context.Students
                .Include(s => s.ClassSection).ThenInclude(cs => cs.ClassLevel)
                .Where(s => s.ClassSection.ClassLevel.SchoolId == user.SchoolId);
            if (sectionId.HasValue) query = query.Where(s => s.ClassSectionId == sectionId.Value);
            else if (classLevelId.HasValue) query = query.Where(s => s.ClassSection.ClassLevelId == classLevelId.Value);

            ViewBag.SelectedClassLevelId = classLevelId;
            ViewBag.SelectedSectionId = sectionId;
            return View(await query.OrderBy(s => s.ClassSection.ClassLevel.Order).ThenBy(s => s.ClassSection.Name).ThenBy(s => s.StudentId).ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> EnrollStudent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            await LoadSectionSelectAsync(user.SchoolId.Value);
            return View(new EnrollStudentViewModel { DateOfBirth = DateTime.Today.AddYears(-10) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(EnrollStudentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!ModelState.IsValid)
            {
                await LoadSectionSelectAsync(user.SchoolId.Value);
                return View(model);
            }

            var section = await _context.ClassSections.Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.ClassSectionId && s.ClassLevel.SchoolId == user.SchoolId);
            if (section == null) return BadRequest();

            _context.Students.Add(new Student
            {
                StudentId = model.StudentId,
                Name = model.Name,
                ClassSectionId = model.ClassSectionId,
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                Address = model.Address,
                Contact = model.Contact,
                BloodGroup = model.BloodGroup,
                GuardianName = model.GuardianName,
                GuardianContact = model.GuardianContact,
                IsActive = true
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Students), new { sectionId = model.ClassSectionId });
        }

        public async Task<IActionResult> StudentProfile(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var student = await GetStudentForSchoolAsync(id, user.SchoolId.Value);
            if (student == null) return NotFound();
            return View(student);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var student = await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.StudentId && s.ClassSection.ClassLevel.SchoolId == user.SchoolId);
            if (student == null) return NotFound();

            _context.StudentComments.Add(new StudentComment
            {
                StudentId = student.Id,
                CommentType = model.CommentType,
                Text = model.Text,
                CreatedByUserId = user.Id,
                ClassSectionId = model.ClassSectionId ?? student.ClassSectionId,
                SubjectId = model.SubjectId
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StudentProfile), new { id = student.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeSection(ChangeSectionViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var student = await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.StudentId && s.ClassSection.ClassLevel.SchoolId == user.SchoolId);
            var newSection = await _context.ClassSections.Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.NewSectionId && s.ClassLevel.SchoolId == user.SchoolId);
            if (student == null || newSection == null || student.ClassSection.ClassLevelId != newSection.ClassLevelId) return BadRequest();

            student.ClassSectionId = newSection.Id;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StudentProfile), new { id = student.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteStudents(PromoteStudentsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var targetSection = await _context.ClassSections.Include(s => s.ClassLevel)
                .FirstOrDefaultAsync(s => s.Id == model.TargetSectionId && s.ClassLevelId == model.TargetClassLevelId && s.ClassLevel.SchoolId == user.SchoolId);
            if (targetSection == null || model.StudentIds.Count == 0) return RedirectToAction(nameof(Students));

            var students = await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .Where(s => model.StudentIds.Contains(s.Id) && s.ClassSection.ClassLevel.SchoolId == user.SchoolId)
                .ToListAsync();
            foreach (var student in students) student.ClassSectionId = targetSection.Id;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Students), new { sectionId = targetSection.Id });
        }

        public async Task<IActionResult> Exams()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            await LoadClassSelectAsync(user.SchoolId.Value);
            var exams = await _context.Exams.Include(e => e.ClassLevel)
                .Where(e => e.SchoolId == user.SchoolId)
                .OrderBy(e => e.ClassLevel.Order).ThenBy(e => e.Name)
                .ToListAsync();
            return View(exams);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExam(CreateExamViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var classLevel = await _context.ClassLevels.FirstOrDefaultAsync(c => c.Id == model.ClassLevelId && c.SchoolId == user.SchoolId);
            if (classLevel == null || !ModelState.IsValid) return RedirectToAction(nameof(Exams));

            _context.Exams.Add(new Exam { Name = model.Name, FullMark = model.FullMark, ClassLevelId = classLevel.Id, SchoolId = user.SchoolId.Value, CreatedByUserId = user.Id });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Exams));
        }

        public async Task<IActionResult> Subscription()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();
            
            var school = await _context.Schools.FindAsync(user.SchoolId);
            if (school == null) return NotFound();

            var plans = await _context.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
            ViewBag.Plans = plans;

            return View(school);
        }

        private async Task<List<ApplicationUser>> GetSchoolTeachersAsync(int schoolId)
        {
            var users = await _context.Users.Where(u => u.SchoolId == schoolId).OrderBy(u => u.FullName).ToListAsync();
            var teachers = new List<ApplicationUser>();
            foreach (var user in users)
                if (await _userManager.IsInRoleAsync(user, "Teacher")) teachers.Add(user);
            return teachers;
        }

        private async Task LoadClassSelectAsync(int schoolId)
        {
            var classes = await _context.ClassLevels.Where(c => c.SchoolId == schoolId).OrderBy(c => c.Order).ToListAsync();
            ViewBag.ClassLevels = new SelectList(classes, "Id", "Name");
        }

        private async Task LoadSectionSelectAsync(int schoolId)
        {
            var sections = await _context.ClassSections.Include(s => s.ClassLevel)
                .Where(s => s.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.ClassLevel.Order).ThenBy(s => s.Name)
                .Select(s => new { s.Id, Label = s.ClassLevel.Name + " - Section " + s.Name })
                .ToListAsync();
            ViewBag.Sections = new SelectList(sections, "Id", "Label");
        }

        private async Task LoadSchoolStructureAsync(int schoolId)
        {
            ViewBag.Classes = await _context.ClassLevels.Include(c => c.Sections).Where(c => c.SchoolId == schoolId).OrderBy(c => c.Order).ToListAsync();
        }

        private async Task<Student?> GetStudentForSchoolAsync(int id, int schoolId)
        {
            return await _context.Students
                .Include(s => s.ClassSection).ThenInclude(cs => cs.ClassLevel).ThenInclude(cl => cl.Sections)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Exam)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Subject)
                .Include(s => s.AttendanceRecords).ThenInclude(a => a.Subject)
                .Include(s => s.Comments).ThenInclude(c => c.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id && s.ClassSection.ClassLevel.SchoolId == schoolId);
        }

        [HttpPost]
        public async Task<IActionResult> InitiatePayment(int planId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.SchoolId == null) return Unauthorized();

            var school = await _context.Schools.FindAsync(user.SchoolId);
            var plan = await _context.SubscriptionPlans.FindAsync(planId);

            if (school == null || plan == null || !plan.IsActive)
                return BadRequest("Invalid plan or school.");

            var transactionId = "TXN" + DateTime.UtcNow.Ticks;

            var payment = new SubscriptionPayment
            {
                SchoolId = school.Id,
                SubscriptionPlanId = plan.Id,
                Amount = plan.Price,
                TransactionId = transactionId,
                Status = "Pending"
            };

            _context.SubscriptionPayments.Add(payment);
            await _context.SaveChangesAsync();

            var gatewayUrl = await _paymentService.InitiatePaymentAsync(
                school.Id, plan.Id, plan.Price,
                transactionId, user.FullName ?? "School Admin",
                user.Email ?? "admin@school.com", school.EIIN ?? "000000");

            if (!string.IsNullOrEmpty(gatewayUrl))
            {
                return Redirect(gatewayUrl);
            }

            // Fallback for development if SslCommerz isn't configured
            payment.Status = "Success";
            payment.CompletedAt = DateTime.UtcNow;
            
            // Calculate new expiry date
            var baseDate = (school.SubscriptionExpiry.HasValue && school.SubscriptionExpiry.Value > DateTime.UtcNow) 
                ? school.SubscriptionExpiry.Value 
                : DateTime.UtcNow;
            
            school.SubscriptionExpiry = baseDate.AddDays(plan.DurationDays).AddMinutes(plan.DurationMinutes);
            payment.NewExpiryDate = school.SubscriptionExpiry;
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Subscription");
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentSuccess([FromForm] string tran_id, [FromForm] string val_id, [FromForm] string amount, [FromForm] string currency)
        {
            var payment = await _context.SubscriptionPayments
                .Include(p => p.School)
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.TransactionId == tran_id);

            if (payment == null) return NotFound();

            var isValid = await _paymentService.ValidatePaymentAsync(val_id, amount, currency);

            if (isValid)
            {
                payment.Status = "Success";
                payment.CompletedAt = DateTime.UtcNow;
                payment.GatewayTransactionId = val_id;

                var school = payment.School;
                var plan = payment.Plan;

                var baseDate = (school.SubscriptionExpiry.HasValue && school.SubscriptionExpiry.Value > DateTime.UtcNow) 
                    ? school.SubscriptionExpiry.Value 
                    : DateTime.UtcNow;
                
                school.SubscriptionExpiry = baseDate.AddDays(plan.DurationDays).AddMinutes(plan.DurationMinutes);
                payment.NewExpiryDate = school.SubscriptionExpiry;

                await _context.SaveChangesAsync();
                return RedirectToAction("Subscription");
            }

            return RedirectToAction("PaymentFail");
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentFail([FromForm] string tran_id)
        {
            var payment = await _context.SubscriptionPayments.FirstOrDefaultAsync(p => p.TransactionId == tran_id);
            if (payment != null)
            {
                payment.Status = "Failed";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Subscription");
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentCancel([FromForm] string tran_id)
        {
            var payment = await _context.SubscriptionPayments.FirstOrDefaultAsync(p => p.TransactionId == tran_id);
            if (payment != null)
            {
                payment.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Subscription");
        }
    }
}
