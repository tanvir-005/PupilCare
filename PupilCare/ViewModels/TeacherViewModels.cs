using System.ComponentModel.DataAnnotations;

namespace PupilCare.ViewModels
{
    public class AddRecordViewModel
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public string Type { get; set; } // Academic, Behavioral etc.
        
        [Required]
        public string Text { get; set; }
    }
}
