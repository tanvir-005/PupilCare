using System.ComponentModel.DataAnnotations;

namespace PupilCare.ViewModels
{
    // ── Auth ──────────────────────────────────────────────────────────────────
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class RegisterSchoolAdminViewModel
    {
        // Admin info
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;
        public string? Designation { get; set; }

        // School info
        [Required] public string SchoolName { get; set; } = string.Empty;
        public string? EIIN { get; set; }
        public string? SchoolAddress { get; set; }
    }

    // ── Class Level ───────────────────────────────────────────────────────────
    public class CreateClassLevelViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Range(1, 20)] public int Order { get; set; }
    }

    public class EditClassLevelViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Range(1, 20)] public int Order { get; set; }
    }

    // ── Section ───────────────────────────────────────────────────────────────
    public class CreateSectionViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public int ClassLevelId { get; set; }
    }

    public class EditSectionViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
    }

    // ── Subject ───────────────────────────────────────────────────────────────
    public class CreateSubjectViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public int ClassLevelId { get; set; }
    }

    public class EditSubjectViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
    }

    // ── Teacher ───────────────────────────────────────────────────────────────
    public class CreateTeacherViewModel
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Designation { get; set; }
    }

    public class EditTeacherViewModel
    {
        public string Id { get; set; } = string.Empty;
        [Required] public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Designation { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignTeacherViewModel
    {
        [Required] public string TeacherId { get; set; } = string.Empty;
        [Required] public int ClassSectionId { get; set; }
        [Required] public int SubjectId { get; set; }
    }

    public class SetClassTeacherViewModel
    {
        [Required] public int ClassLevelId { get; set; }
        public string? TeacherId { get; set; }  // null = remove class teacher
    }

    // ── Student ───────────────────────────────────────────────────────────────
    public class EnrollStudentViewModel
    {
        [Required] public string StudentId { get; set; } = string.Empty;
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public int ClassSectionId { get; set; }
        [Required] public string Gender { get; set; } = string.Empty;
        [DataType(DataType.Date)] public System.DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public string? BloodGroup { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianContact { get; set; }
    }

    public class EditStudentViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Gender { get; set; } = string.Empty;
        [DataType(DataType.Date)] public System.DateTime DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? Contact { get; set; }
        public string? BloodGroup { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianContact { get; set; }
        public bool IsActive { get; set; }
    }

    public class ChangeSectionViewModel
    {
        [Required] public int StudentId { get; set; }
        [Required] public int NewSectionId { get; set; }
    }

    public class PromoteStudentsViewModel
    {
        public int TargetClassLevelId { get; set; }
        public int TargetSectionId { get; set; }
        public System.Collections.Generic.List<int> StudentIds { get; set; } = new();
    }

    // ── Exam ──────────────────────────────────────────────────────────────────
    public class CreateExamViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(1, 1000)] public int FullMark { get; set; }
        [Required] public int ClassLevelId { get; set; }
    }

    public class EditExamViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(1, 1000)] public int FullMark { get; set; }
    }

    // ── Marks ─────────────────────────────────────────────────────────────────
    public class GradeStudentsViewModel
    {
        public int ExamId { get; set; }
        public int SubjectId { get; set; }
        public int ClassSectionId { get; set; }
        public System.Collections.Generic.List<StudentMarkEntry> Entries { get; set; } = new();
    }

    public class StudentMarkEntry
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentRollId { get; set; } = string.Empty;
        public int? ExamMarkId { get; set; }  // null = not graded yet
        [Range(0, 1000)] public decimal? MarksObtained { get; set; }
    }

    // ── Attendance ────────────────────────────────────────────────────────────
    public class TakeAttendanceViewModel
    {
        public int SubjectId { get; set; }
        public int ClassSectionId { get; set; }
        [DataType(DataType.Date)] public System.DateTime Date { get; set; } = System.DateTime.Today;
        public System.Collections.Generic.List<AttendanceEntry> Entries { get; set; } = new();
    }

    public class AttendanceEntry
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentRollId { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public int? ExistingRecordId { get; set; }
    }

    // ── Comment ───────────────────────────────────────────────────────────────
    public class AddCommentViewModel
    {
        [Required] public int StudentId { get; set; }
        [Required] public string CommentType { get; set; } = string.Empty;
        [Required] public string Text { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public int? ClassSectionId { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class EditCommentViewModel
    {
        public int Id { get; set; }
        [Required] public string CommentType { get; set; } = string.Empty;
        [Required] public string Text { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    // ── AI Insights ───────────────────────────────────────────────────────────
    public class GenerateInsightViewModel
    {
        [Required] public string InsightScope { get; set; } = string.Empty;
        public int? StudentId { get; set; }
        public int? ClassLevelId { get; set; }
        public int? ClassSectionId { get; set; }
        public int? SubjectId { get; set; }
    }

    // ── Subscription ──────────────────────────────────────────────────────────
    public class InitiatePaymentViewModel
    {
        [Required] public int PlanId { get; set; }
    }

    // ── Super Admin ───────────────────────────────────────────────────────────
    public class EditSubscriptionPlanViewModel
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(0, 1000000)] public decimal Price { get; set; }
        [Range(0, 3650)] public int DurationDays { get; set; }
        [Range(0, 1440)] public int DurationMinutes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubscriptionPlanViewModel
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required, Range(0, 1000000)] public decimal Price { get; set; }
        [Range(0, 3650)] public int DurationDays { get; set; }
        [Range(0, 1440)] public int DurationMinutes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
