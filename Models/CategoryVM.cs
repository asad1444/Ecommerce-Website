using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class CategoryVM
    {
        public int CategoryId { get; set; }
        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;
        public IFormFile? CategoryPhoto { get; set; }

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
