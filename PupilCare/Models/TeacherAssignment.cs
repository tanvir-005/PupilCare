using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    /// <summary>
    /// Links a Teacher to a specific ClassSection and Subject.
    /// One teacher can have multiple assignments across sections and subjects.
    /// Unique constraint: one teacher per section-subject combination.
    /// </summary>
    public class TeacherAssignment
    {
        public int Id { get; set; }

        [Required]
        public string TeacherId { get; set; } = string.Empty;
        public ApplicationUser Teacher { get; set; } = null!;

        public int ClassSectionId { get; set; }
        public ClassSection ClassSection { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}
