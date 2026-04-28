using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// A section within a class level (e.g. "A", "B", "C").
    /// Students are enrolled per section, and teachers are assigned per section+subject.
    /// </summary>
    public class ClassSection
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;     // "A", "B", "C"

        public int ClassLevelId { get; set; }
        public ClassLevel ClassLevel { get; set; } = null!;

        public ICollection<Student> Students { get; set; } = new List<Student>();
        public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<StudentComment> Comments { get; set; } = new List<StudentComment>();
    }
}
