using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// A subject (course) belonging to a class level.
    /// Subjects are shared across all sections of that class.
    /// e.g. "Bangla", "English", "Math" in Class Six.
    /// </summary>
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int ClassLevelId { get; set; }
        public ClassLevel ClassLevel { get; set; } = null!;

        public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public ICollection<ExamMark> ExamMarks { get; set; } = new List<ExamMark>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<StudentComment> Comments { get; set; } = new List<StudentComment>();
    }
}
