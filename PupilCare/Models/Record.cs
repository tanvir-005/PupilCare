using System;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class Record
    {
        public int Id { get; set; }
        
        [Required]
        public string Type { get; set; } // e.g., Academic, Behavioral
        
        [Required]
        public string Text { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public string TeacherId { get; set; }
        public ApplicationUser Teacher { get; set; }
    }
}
