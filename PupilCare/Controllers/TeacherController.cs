using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PupilCare.Data;
using PupilCare.Filters;
using PupilCare.Models;
using PupilCare.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PupilCare.Controllers
{
    [Authorize(Roles = "Teacher")]
    [WriteGuard]
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

            var assignments = await _context.TeacherAssignments
                .Include(ta => ta.ClassSection)
                    .ThenInclude(cs => cs.ClassLevel)
                .Include(ta => ta.Subject)
                .Where(ta => ta.TeacherId == user.Id)
                .ToListAsync();

            var classTeacherFor = await _context.ClassLevels
                .Where(cl => cl.ClassTeacherId == user.Id)
                .ToListAsync();

            ViewBag.ClassTeacherFor = classTeacherFor;

            return View(assignments);
        }

        public async Task<IActionResult> SectionSubject(int sectionId, int subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var assignment = await _context.TeacherAssignments
                .Include(ta => ta.ClassSection)
                    .ThenInclude(cs => cs.ClassLevel)
                .Include(ta => ta.Subject)
                .FirstOrDefaultAsync(ta => ta.TeacherId == user.Id && ta.ClassSectionId == sectionId && ta.SubjectId == subjectId);

            if (assignment == null) return NotFound("You are not assigned to this section and subject.");

            var students = await _context.Students
                .Where(s => s.ClassSectionId == sectionId)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> TakeAttendance(int sectionId, int subjectId, DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!await CanAccessSectionSubjectAsync(user, sectionId, subjectId)) return Forbid();

            var attendanceDate = (date ?? DateTime.Today).Date;
            var students = await _context.Students.Where(s => s.ClassSectionId == sectionId && s.IsActive).OrderBy(s => s.StudentId).ToListAsync();
            var existing = await _context.AttendanceRecords
                .Where(a => a.ClassSectionId == sectionId && a.SubjectId == subjectId && a.Date == attendanceDate)
                .ToListAsync();

            var model = new TakeAttendanceViewModel
            {
                ClassSectionId = sectionId,
                SubjectId = subjectId,
                Date = attendanceDate,
                Entries = students.Select(s =>
                {
                    var record = existing.FirstOrDefault(a => a.StudentId == s.Id);
                    return new AttendanceEntry
                    {
                        StudentId = s.Id,
                        StudentName = s.Name,
                        StudentRollId = s.StudentId,
                        IsPresent = record?.IsPresent ?? true,
                        ExistingRecordId = record?.Id
                    };
                }).ToList()
            };

            await LoadAssignmentLabelsAsync(sectionId, subjectId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeAttendance(TakeAttendanceViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!await CanAccessSectionSubjectAsync(user, model.ClassSectionId, model.SubjectId)) return Forbid();

            var date = model.Date.Date;
            foreach (var entry in model.Entries)
            {
                var record = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(a => a.StudentId == entry.StudentId && a.SubjectId == model.SubjectId && a.Date == date);
                if (record == null)
                {
                    _context.AttendanceRecords.Add(new AttendanceRecord
                    {
                        StudentId = entry.StudentId,
                        SubjectId = model.SubjectId,
                        ClassSectionId = model.ClassSectionId,
                        Date = date,
                        IsPresent = entry.IsPresent,
                        TakenByUserId = user.Id
                    });
                }
                else
                {
                    record.IsPresent = entry.IsPresent;
                    record.TakenByUserId = user.Id;
                    record.RecordedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(SectionSubject), new { sectionId = model.ClassSectionId, subjectId = model.SubjectId });
        }

        [HttpGet]
        public async Task<IActionResult> GradeStudents(int sectionId, int subjectId, int? examId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!await CanAccessSectionSubjectAsync(user, sectionId, subjectId)) return Forbid();

            var section = await _context.ClassSections.Include(s => s.ClassLevel).FirstOrDefaultAsync(s => s.Id == sectionId);
            if (section == null) return NotFound();
            var exams = await _context.Exams.Where(e => e.ClassLevelId == section.ClassLevelId).OrderBy(e => e.Name).ToListAsync();
            var selectedExamId = examId ?? exams.FirstOrDefault()?.Id ?? 0;
            ViewBag.Exams = new SelectList(exams, "Id", "Name", selectedExamId);

            var students = await _context.Students.Where(s => s.ClassSectionId == sectionId && s.IsActive).OrderBy(s => s.StudentId).ToListAsync();
            var marks = selectedExamId == 0
                ? new System.Collections.Generic.List<ExamMark>()
                : await _context.ExamMarks.Where(m => m.ExamId == selectedExamId && m.SubjectId == subjectId).ToListAsync();

            var model = new GradeStudentsViewModel
            {
                ClassSectionId = sectionId,
                SubjectId = subjectId,
                ExamId = selectedExamId,
                Entries = students.Select(s =>
                {
                    var mark = marks.FirstOrDefault(m => m.StudentId == s.Id);
                    return new StudentMarkEntry
                    {
                        StudentId = s.Id,
                        StudentName = s.Name,
                        StudentRollId = s.StudentId,
                        ExamMarkId = mark?.Id,
                        MarksObtained = mark?.MarksObtained
                    };
                }).ToList()
            };

            await LoadAssignmentLabelsAsync(sectionId, subjectId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeStudents(GradeStudentsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();
            if (!await CanAccessSectionSubjectAsync(user, model.ClassSectionId, model.SubjectId)) return Forbid();

            foreach (var entry in model.Entries)
            {
                var mark = await _context.ExamMarks.FirstOrDefaultAsync(m => m.ExamId == model.ExamId && m.StudentId == entry.StudentId && m.SubjectId == model.SubjectId);
                if (mark == null)
                {
                    _context.ExamMarks.Add(new ExamMark
                    {
                        ExamId = model.ExamId,
                        StudentId = entry.StudentId,
                        SubjectId = model.SubjectId,
                        MarksObtained = entry.MarksObtained,
                        GradedByUserId = user.Id,
                        GradedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    mark.MarksObtained = entry.MarksObtained;
                    mark.GradedByUserId = user.Id;
                    mark.GradedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(SectionSubject), new { sectionId = model.ClassSectionId, subjectId = model.SubjectId });
        }

        [HttpGet]
        public async Task<IActionResult> StudentProfile(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SchoolId == null) return Unauthorized();

            var student = await _context.Students
                .Include(s => s.ClassSection).ThenInclude(cs => cs.ClassLevel).ThenInclude(cl => cl.Sections)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Exam)
                .Include(s => s.ExamMarks).ThenInclude(m => m.Subject)
                .Include(s => s.AttendanceRecords).ThenInclude(a => a.Subject)
                .Include(s => s.Comments).ThenInclude(c => c.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id && s.ClassSection.ClassLevel.SchoolId == user.SchoolId);
            if (student == null) return NotFound();
            if (!await CanAccessStudentAsync(user, student)) return Forbid();

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
            if (!await CanAccessStudentAsync(user, student)) return Forbid();

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
            return LocalRedirect(model.ReturnUrl ?? Url.Action(nameof(StudentProfile), new { id = student.Id })!);
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
            if (student.ClassSection.ClassLevel.ClassTeacherId != user.Id) return Forbid();

            student.ClassSectionId = newSection.Id;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StudentProfile), new { id = student.Id });
        }

        private async Task<bool> CanAccessSectionSubjectAsync(ApplicationUser user, int sectionId, int subjectId)
        {
            var assigned = await _context.TeacherAssignments.AnyAsync(a => a.TeacherId == user.Id && a.ClassSectionId == sectionId && a.SubjectId == subjectId);
            if (assigned) return true;

            return await _context.ClassSections.Include(s => s.ClassLevel)
                .AnyAsync(s => s.Id == sectionId && s.ClassLevel.ClassTeacherId == user.Id && s.ClassLevel.Subjects.Any(sub => sub.Id == subjectId));
        }

        private async Task<bool> CanAccessStudentAsync(ApplicationUser user, Student student)
        {
            if (student.ClassSection.ClassLevel.ClassTeacherId == user.Id) return true;
            return await _context.TeacherAssignments.AnyAsync(a => a.TeacherId == user.Id && a.ClassSectionId == student.ClassSectionId);
        }

        private async Task LoadAssignmentLabelsAsync(int sectionId, int subjectId)
        {
            ViewBag.Section = await _context.ClassSections.Include(s => s.ClassLevel).FirstOrDefaultAsync(s => s.Id == sectionId);
            ViewBag.Subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId);
        }
    }
}
