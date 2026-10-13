using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class AdminProfileViewModel
    {
        [Required, StringLength(120)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        public string Email { get; set; } = string.Empty;
    }
}
