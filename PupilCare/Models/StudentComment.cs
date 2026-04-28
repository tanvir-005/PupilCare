using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// A typed comment on a student — replaces the old flat Record model.
    /// Types: Behavior, Result, Attendance, Financial, General
    /// </summary>
    public class StudentComment
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Required]
        public string CommentType { get; set; } = string.Empty;
        // "Behavior" | "Result" | "Attendance" | "Financial" | "General"

        [Required]
        public string Text { get; set; } = string.Empty;

        public string CreatedByUserId { get; set; } = string.Empty;
        public ApplicationUser CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional context — which subject/section triggered this comment
        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? ClassSectionId { get; set; }
        public ClassSection? ClassSection { get; set; }
    }
}
