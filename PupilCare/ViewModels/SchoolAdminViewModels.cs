using System.ComponentModel.DataAnnotations;

namespace PupilCare.ViewModels
{
    public class CreateClassroomViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string GradeLevel { get; set; }
    }

    public class CreateTeacherViewModel
    {
        [Required]
        public string FullName { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class EnrollStudentViewModel
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string StudentId { get; set; }

        public string Address { get; set; }
        public string Contact { get; set; }
        
        [Required]
        public string Gender { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public System.DateTime DateOfBirth { get; set; }

        [Required]
        public int ClassroomId { get; set; }
    }
}
