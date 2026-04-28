using Microsoft.AspNetCore.Identity;

namespace PupilCare.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public int? SchoolId { get; set; }
        public School? School { get; set; }
        public string? Phone { get; set; }
        public string? Designation { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
