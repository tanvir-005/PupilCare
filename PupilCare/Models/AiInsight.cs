using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// Stores AI-generated insights at various scopes.
    /// InsightScope values:
    ///   "Student"           — single student
    ///   "SectionSubject"    — all students in a section for one subject
    ///   "ClassSubject"      — all students across all sections for one subject
    ///   "Section"           — all subjects for all students in a section
    ///   "Class"             — all subjects, all sections in a class
    ///   "School"            — entire school
    /// </summary>
    public class AiInsight
    {
        public int Id { get; set; }

        [Required]
        public string InsightScope { get; set; } = string.Empty;

        /// <summary>Human-readable label like "Class Six - Section A - Bangla"</summary>
        public string ScopeLabel { get; set; } = string.Empty;

        [Required]
        public string GeneratedText { get; set; } = string.Empty;

        public string GeneratedByUserId { get; set; } = string.Empty;
        public ApplicationUser GeneratedBy { get; set; } = null!;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public int SchoolId { get; set; }
        public School School { get; set; } = null!;

        // Scope identifiers (nullable — only the relevant ones are set)
        public int? StudentId { get; set; }
        public int? ClassLevelId { get; set; }
        public int? ClassSectionId { get; set; }
        public int? SubjectId { get; set; }
    }
}
