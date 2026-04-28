using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// Attendance for one student in one subject on a specific date.
    /// Teachers record attendance per class-section-subject.
    /// Unique per (StudentId, SubjectId, Date).
    /// </summary>
    public class AttendanceRecord
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public int ClassSectionId { get; set; }
        public ClassSection ClassSection { get; set; } = null!;

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        public bool IsPresent { get; set; }

        public string TakenByUserId { get; set; } = string.Empty;
        public ApplicationUser TakenBy { get; set; } = null!;

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
