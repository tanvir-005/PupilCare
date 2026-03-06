using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class School
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public bool IsApproved { get; set; } = false;

        public ICollection<Classroom> Classrooms { get; set; }
        public ICollection<ApplicationUser> Users { get; set; }
    }
}
