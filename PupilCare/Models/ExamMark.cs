using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// A student's mark in a specific exam for a specific subject.
    /// Unique per (ExamId, StudentId, SubjectId).
    /// </summary>
    public class ExamMark
    {
        public int Id { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; } = null!;

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        [Range(0, 1000)]
        public decimal? MarksObtained { get; set; }

        public string? GradedByUserId { get; set; }
        public ApplicationUser? GradedBy { get; set; }
        public DateTime? GradedAt { get; set; }
    }
}
