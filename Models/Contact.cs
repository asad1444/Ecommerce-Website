using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class Contact
    {
        public int ContactId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

}
