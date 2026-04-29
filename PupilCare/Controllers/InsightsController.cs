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
            NormalizeInsightSelection(model);
            await ValidateInsightSelectionAsync(user.SchoolId.Value, model);
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
            var students = await _context.Students.Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .Where(s => s.ClassSection.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.StudentId)
                .Select(s => new { s.Id, s.StudentId, s.Name, s.ClassSectionId, Label = s.StudentId + " - " + s.Name })
                .ToListAsync();
            var classes = await _context.ClassLevels.Where(c => c.SchoolId == schoolId).OrderBy(c => c.Order).ToListAsync();
            var sections = await _context.ClassSections.Include(s => s.ClassLevel)
                .Where(s => s.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.ClassLevel.Order).ThenBy(s => s.Name)
                .Select(s => new { s.Id, s.ClassLevelId, s.Name, Label = s.ClassLevel.Name + " - Section " + s.Name })
                .ToListAsync();
            var subjects = await _context.Subjects.Include(s => s.ClassLevel)
                .Where(s => s.ClassLevel.SchoolId == schoolId)
                .OrderBy(s => s.ClassLevel.Order).ThenBy(s => s.Name)
                .Select(s => new { s.Id, s.ClassLevelId, s.Name, Label = s.ClassLevel.Name + " - " + s.Name })
                .ToListAsync();

            ViewBag.Students = new SelectList(students, "Id", "Label");
            ViewBag.Classes = new SelectList(classes, "Id", "Name");
            ViewBag.Sections = new SelectList(sections, "Id", "Label");
            ViewBag.Subjects = new SelectList(subjects, "Id", "Label");
            ViewBag.ClassOptions = classes.Select(c => new { c.Id, c.Name });
            ViewBag.SectionOptions = sections;
            ViewBag.SubjectOptions = subjects;
            ViewBag.StudentOptions = students;
        }

        private void NormalizeInsightSelection(GenerateInsightViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.InsightScope)) model.InsightScope = "School";

            if (model.InsightScope == "School")
            {
                model.ClassLevelId = null;
                model.ClassSectionId = null;
                model.SubjectId = null;
                model.StudentId = null;
            }
            else if (model.InsightScope == "Class")
            {
                model.ClassSectionId = null;
                model.SubjectId = null;
                model.StudentId = null;
            }
            else if (model.InsightScope == "Section")
            {
                model.SubjectId = null;
                model.StudentId = null;
            }
            else if (model.InsightScope == "SectionSubject")
            {
                model.StudentId = null;
            }
        }

        private async Task ValidateInsightSelectionAsync(int schoolId, GenerateInsightViewModel model)
        {
            var validScopes = new[] { "School", "Class", "Section", "SectionSubject", "Student" };
            if (!validScopes.Contains(model.InsightScope))
            {
                ModelState.AddModelError(nameof(model.InsightScope), "Choose what the insight should cover.");
                return;
            }

            if (model.InsightScope == "School") return;

            if (!model.ClassLevelId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ClassLevelId), "Choose a class.");
                return;
            }

            var classExists = await _context.ClassLevels.AnyAsync(c => c.Id == model.ClassLevelId && c.SchoolId == schoolId);
            if (!classExists)
            {
                ModelState.AddModelError(nameof(model.ClassLevelId), "Choose a valid class for this school.");
                return;
            }

            if (model.InsightScope == "Class") return;

            if (!model.ClassSectionId.HasValue)
            {
                ModelState.AddModelError(nameof(model.ClassSectionId), "Choose a section.");
                return;
            }

            var sectionExists = await _context.ClassSections
                .AnyAsync(s => s.Id == model.ClassSectionId && s.ClassLevelId == model.ClassLevelId);
            if (!sectionExists)
            {
                ModelState.AddModelError(nameof(model.ClassSectionId), "Choose a valid section for the selected class.");
                return;
            }

            if (model.InsightScope == "Section") return;

            if (!model.SubjectId.HasValue)
            {
                ModelState.AddModelError(nameof(model.SubjectId), "Choose a subject.");
                return;
            }

            var subjectExists = await _context.Subjects
                .AnyAsync(s => s.Id == model.SubjectId && s.ClassLevelId == model.ClassLevelId);
            if (!subjectExists)
            {
                ModelState.AddModelError(nameof(model.SubjectId), "Choose a valid subject for the selected class.");
                return;
            }

            if (model.InsightScope == "SectionSubject") return;

            if (!model.StudentId.HasValue)
            {
                ModelState.AddModelError(nameof(model.StudentId), "Choose a student.");
                return;
            }

            var studentExists = await _context.Students
                .AnyAsync(s => s.Id == model.StudentId && s.ClassSectionId == model.ClassSectionId);
            if (!studentExists)
            {
                ModelState.AddModelError(nameof(model.StudentId), "Choose a valid student for the selected section.");
            }
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
            var studentsQuery = _context.Students
                .Include(s => s.ClassSection).ThenInclude(s => s.ClassLevel)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Exam)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Subject)
                .Include(s => s.AttendanceRecords).ThenInclude(a => a.Subject)
                .Include(s => s.Comments)
                .ThenInclude(c => c.Subject)
                .Where(s => s.ClassSection.ClassLevel.SchoolId == schoolId);

            if (model.ClassLevelId.HasValue) studentsQuery = studentsQuery.Where(s => s.ClassSection.ClassLevelId == model.ClassLevelId);
            if (model.ClassSectionId.HasValue) studentsQuery = studentsQuery.Where(s => s.ClassSectionId == model.ClassSectionId);
            if (model.StudentId.HasValue) studentsQuery = studentsQuery.Where(s => s.Id == model.StudentId);

            var subject = model.SubjectId.HasValue ? await _context.Subjects.FindAsync(model.SubjectId.Value) : null;
            var list = await studentsQuery.OrderBy(s => s.ClassSection.ClassLevel.Order).ThenBy(s => s.ClassSection.Name).ThenBy(s => s.StudentId).ToListAsync();

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

            var studentRecords = list.Select(s =>
            {
                var marks = s.ExamMarks
                    .Where(m => subject == null || m.SubjectId == subject.Id)
                    .OrderBy(m => m.Exam.Name)
                    .ThenBy(m => m.Subject.Name)
                    .Select(m => new
                    {
                        Exam = m.Exam.Name,
                        Subject = m.Subject.Name,
                        m.MarksObtained,
                        m.Exam.FullMark,
                        Percentage = m.MarksObtained.HasValue && m.Exam.FullMark > 0
                            ? Math.Round((double)(m.MarksObtained.Value / m.Exam.FullMark * 100), 2)
                            : (double?)null,
                        m.GradedAt
                    });

                var attendanceRecords = s.AttendanceRecords
                    .Where(a => subject == null || a.SubjectId == subject.Id)
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.Date,
                        Subject = a.Subject.Name,
                        Status = a.IsPresent ? "Present" : "Absent",
                        a.RecordedAt
                    });

                var comments = s.Comments
                    .Where(c => subject == null || c.SubjectId == null || c.SubjectId == subject.Id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.CommentType,
                        c.Text,
                        Subject = c.Subject == null ? null : c.Subject.Name,
                        c.CreatedAt
                    });

                return new
                {
                    Profile = new
                    {
                        s.StudentId,
                        s.Name,
                        s.Gender,
                        s.DateOfBirth,
                        s.BloodGroup,
                        Guardian = s.GuardianName,
                        GuardianContact = s.GuardianContact,
                        Class = s.ClassSection.ClassLevel.Name,
                        Section = s.ClassSection.Name,
                        s.IsActive
                    },
                    AcademicRecords = marks,
                    AttendanceSummary = s.AttendanceRecords
                        .Where(a => subject == null || a.SubjectId == subject.Id)
                        .GroupBy(a => a.Subject.Name)
                        .Select(g => new
                        {
                            Subject = g.Key,
                            Present = g.Count(a => a.IsPresent),
                            Absent = g.Count(a => !a.IsPresent),
                            Total = g.Count(),
                            AttendanceRate = g.Count() == 0 ? 0 : Math.Round(g.Count(a => a.IsPresent) * 100.0 / g.Count(), 2)
                        }),
                    AttendanceRecords = attendanceRecords,
                    Comments = comments
                };
            }).ToList();

            var allMarks = list.SelectMany(s => s.ExamMarks.Where(m => subject == null || m.SubjectId == subject.Id)).ToList();
            var allAttendance = list.SelectMany(s => s.AttendanceRecords.Where(a => subject == null || a.SubjectId == subject.Id)).ToList();
            var data = new
            {
                Request = new
                {
                    Scope = model.InsightScope,
                    Label = label,
                    GeneratedFor = new
                    {
                        SchoolId = schoolId,
                        model.ClassLevelId,
                        model.ClassSectionId,
                        model.SubjectId,
                        Subject = subject?.Name,
                        model.StudentId
                    }
                },
                Summary = new
                {
                    StudentCount = list.Count,
                    MarkRecordCount = allMarks.Count,
                    AttendanceRecordCount = allAttendance.Count,
                    CommentCount = list.Sum(s => s.Comments.Count(c => subject == null || c.SubjectId == null || c.SubjectId == subject.Id)),
                    AverageMarkPercentage = allMarks.Any(m => m.MarksObtained.HasValue && m.Exam.FullMark > 0)
                        ? Math.Round(allMarks.Where(m => m.MarksObtained.HasValue && m.Exam.FullMark > 0).Average(m => (double)(m.MarksObtained!.Value / m.Exam.FullMark * 100)), 2)
                        : (double?)null,
                    OverallAttendanceRate = allAttendance.Count == 0 ? (double?)null : Math.Round(allAttendance.Count(a => a.IsPresent) * 100.0 / allAttendance.Count, 2)
                },
                Students = studentRecords
            };

            return (label ?? "Insight", data);
        }
    }
}
