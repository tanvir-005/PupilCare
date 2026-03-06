using Microsoft.AspNetCore.Identity;

namespace PupilCare.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public int? SchoolId { get; set; }
        
        public School School { get; set; }
    }
}
