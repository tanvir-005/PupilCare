using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class Student
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;   // User-defined unique ID per school

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? Contact { get; set; }
        public string? Gender { get; set; }
        public string? BloodGroup { get; set; }
        public string? GuardianName { get; set; }
        public string? GuardianContact { get; set; }
        public string? Photo { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public bool IsActive { get; set; } = true;

        // Section enrollment (section implies class level)
        public int ClassSectionId { get; set; }
        public ClassSection ClassSection { get; set; } = null!;

        public ICollection<ExamMark> ExamMarks { get; set; } = new List<ExamMark>();
        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<StudentComment> Comments { get; set; } = new List<StudentComment>();
    }
}
