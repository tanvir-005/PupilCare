using System.ComponentModel.DataAnnotations;

namespace PupilCare.ViewModels
{
    public class RegisterSchoolAdminViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string SchoolName { get; set; }
        
        [Required]
        public string SchoolAddress { get; set; }
        
        [Required]
        public string SchoolContact { get; set; }
    }
}
