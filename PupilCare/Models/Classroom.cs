using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class Classroom
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string GradeLevel { get; set; }
        
        public int SchoolId { get; set; }
        public School School { get; set; }

        public ICollection<Student> Students { get; set; }
    }
}
