using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Filters;
using PupilCare.Models;
using PupilCare.Services;
using PupilCare.ViewModels;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "SchoolAdmin,Teacher")]
    [WriteGuard]
    public class InsightsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAiInsightService _insightService;

        public InsightsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAiInsightService insightService)
        {
            _context = context;
            _userManager = userManager;
            _insightService = insightService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();

            var insights = await _context.AiInsights
                .Include(i => i.GeneratedBy)
                .Where(i => i.SchoolId == user.SchoolId)
                .OrderByDescending(i => i.GeneratedAt)
                .ToListAsync();

            return View(insights);
        }

        [HttpGet]
        public async Task<IActionResult> Generate()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            await LoadSelectsAsync(user.SchoolId.Value);
            return View(new GenerateInsightViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GenerateInsightViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!ModelState.IsValid)
            {
                await LoadSelectsAsync(user.SchoolId.Value);
                return View(model);
            }

            if (!await CanGenerateAsync(user, model)) return Forbid();

            var (label, data) = await BuildInsightDataAsync(user.SchoolId.Value, model);
            var text = await _insightService.GenerateInsightAsync(model.InsightScope, label, data);

            var insight = new AiInsight
            {
                InsightScope = model.InsightScope,
                ScopeLabel = label,
                GeneratedText = text,
                GeneratedByUserId = user.Id,
                SchoolId = user.SchoolId.Value,
                StudentId = model.StudentId,
                ClassLevelId = model.ClassLevelId,
                ClassSectionId = model.ClassSectionId,
                SubjectId = model.SubjectId
            };

            _context.AiInsights.Add(insight);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = insight.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            var insight = await _context.AiInsights.Include(i => i.GeneratedBy)
                .FirstOrDefaultAsync(i => i.Id == id && i.SchoolId == user.SchoolId);
            if (insight == null) return NotFound();
            return View(insight);
        }

        private async Task LoadSelectsAsync(int schoolId)
        {
            ViewBag.Students = new SelectList(await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .Where(s => s.ClassSection.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.StudentId)
                .Select(s => new { s.Id, Label = s.StudentId + " - " + s.Name })
                .ToListAsync(), "Id", "Label");
            ViewBag.Classes = new SelectList(await _context.ClassLevels.Where(c => c.SchoolId == schoolId).OrderBy(c => c.Order).ToListAsync(), "Id", "Name");
            ViewBag.Sections = new SelectList(await _context.ClassSections.Include(s => s.ClassLevel)
                .Where(s => s.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.ClassLevel.Order).ThenBy(s => s.Name)
                .Select(s => new { s.Id, Label = s.ClassLevel.Name + " - Section " + s.Name })
                .ToListAsync(), "Id", "Label");
            ViewBag.Subjects = new SelectList(await _context.Subjects.Include(s => s.ClassLevel)
                .Where(s => s.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.ClassLevel.Order).ThenBy(s => s.Name)
                .Select(s => new { s.Id, Label = s.ClassLevel.Name + " - " + s.Name })
                .ToListAsync(), "Id", "Label");
        }

        private async Task<bool> CanGenerateAsync(ApplicationUser user, GenerateInsightViewModel model)
        {
            if (await _userManager.IsInRoleAsync(user, "SchoolAdmin")) return true;
            if (model.InsightScope == "School" || model.InsightScope == "Class" || model.InsightScope == "Section") return false;

            if (model.StudentId.HasValue)
            {
                var student = await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                    .FirstOrDefaultAsync(s => s.Id == model.StudentId && s.ClassSection.ClassLevel.SchoolId == user.SchoolId);
                if (student == null) return false;
                if (student.ClassSection.ClassLevel.ClassTeacherId == user.Id) return true;
                return await _context.TeacherAssignments.AnyAsync(a => a.TeacherId == user.Id && a.ClassSectionId == student.ClassSectionId);
            }

            if (model.ClassSectionId.HasValue && model.SubjectId.HasValue)
                return await _context.TeacherAssignments.AnyAsync(a => a.TeacherId == user.Id && a.ClassSectionId == model.ClassSectionId && a.SubjectId == model.SubjectId);

            if (model.ClassLevelId.HasValue && model.SubjectId.HasValue)
                return await _context.ClassLevels.AnyAsync(c => c.Id == model.ClassLevelId && c.ClassTeacherId == user.Id);

            return false;
        }

        private async Task<(string Label, object Data)> BuildInsightDataAsync(int schoolId, GenerateInsightViewModel model)
        {
            var students = _context.Students
                .Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Exam)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Subject)
                .Include(s => s.AttendanceRecords).ThenInclude(a => a.Subject)
                .Include(s => s.Comments)
                .Where(s => s.ClassSection.ClassLevel.SchoolId == schoolId);

            if (model.StudentId.HasValue) students = students.Where(s => s.Id == model.StudentId);
            if (model.ClassLevelId.HasValue) students = students.Where(s => s.ClassSection.ClassLevelId == model.ClassLevelId);
            if (model.ClassSectionId.HasValue) students = students.Where(s => s.ClassSectionId == model.ClassSectionId);

            var subject = model.SubjectId.HasValue ? await _context.Subjects.FindAsync(model.SubjectId.Value) : null;
            var list = await students.ToListAsync();

            var label = model.InsightScope;
            if (list.Count == 1 && model.InsightScope == "Student") label = $"{list[0].StudentId} - {list[0].Name}";
            else if (model.ClassSectionId.HasValue)
            {
                var section = await _context.ClassSections.Include(s => s.ClassLevel).FirstAsync(s => s.Id == model.ClassSectionId);
                label = $"{section.ClassLevel.Name} - Section {section.Name}" + (subject == null ? "" : $" - {subject.Name}");
            }
            else if (model.ClassLevelId.HasValue)
            {
                var cls = await _context.ClassLevels.FindAsync(model.ClassLevelId.Value);
                label = cls?.Name + (subject == null ? "" : $" - {subject.Name}");
            }
            else label = "Entire school";

            var data = list.Select(s => new
            {
                s.StudentId,
                s.Name,
                Class = s.ClassSection.ClassLevel.Name,
                Section = s.ClassSection.Name,
                Marks = s.ExamMarks.Where(m => subject == null || m.SubjectId == subject.Id).Select(m => new { Exam = m.Exam.Name, Subject = m.Subject.Name, m.MarksObtained, m.Exam.FullMark }),
                Attendance = s.AttendanceRecords.Where(a => subject == null || a.SubjectId == subject.Id).GroupBy(a => a.Subject.Name).Select(g => new { Subject = g.Key, Present = g.Count(a => a.IsPresent), Total = g.Count() }),
                Comments = s.Comments.Select(c => new { c.CommentType, c.Text, c.CreatedAt })
            });

            return (label ?? "Insight", data);
        }
    }
}
