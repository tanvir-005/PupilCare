using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// Represents a grade level within a school (e.g. "Class Six", "Class Seven").
    /// Replaces the old flat Classroom model.
    /// </summary>
    public class ClassLevel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;     // "Six", "Seven", "Eight"

        public int Order { get; set; }                        // For promotion ordering (6, 7, 8 ...)

        public int SchoolId { get; set; }
        public School School { get; set; } = null!;

        // Class teacher is a teacher who has school-admin-like access within this class
        public string? ClassTeacherId { get; set; }
        public ApplicationUser? ClassTeacher { get; set; }

        public ICollection<ClassSection> Sections { get; set; } = new List<ClassSection>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
