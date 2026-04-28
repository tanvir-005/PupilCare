using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// An exam created by the school admin for a class level.
    /// Applied across all subjects and sections of that class.
    /// Examples: "First Term", "Mid Term", "Quiz 1", "Final"
    /// </summary>
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int FullMark { get; set; }

        public int ClassLevelId { get; set; }
        public ClassLevel ClassLevel { get; set; } = null!;

        public int SchoolId { get; set; }
        public School School { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedByUserId { get; set; } = string.Empty;

        public ICollection<ExamMark> Marks { get; set; } = new List<ExamMark>();
    }
}
