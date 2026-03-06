using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PupilCare.Models
{
    public class Student
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public string Gender { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; }

        public ICollection<Record> Records { get; set; }
    }
}
