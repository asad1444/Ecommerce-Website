using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace loginlogout.Models
{
    public class ProductVM
    {
        public int ProductId { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "99999999")]
        public decimal Price { get; set; }
        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public IFormFile? ProductPhoto { get; set; }

        [StringLength(4000)]
        public string LongDescription { get; set; } = string.Empty;

        // Foreign Keys
        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }
      

        // Age-wise Stock Inputs from Form
       
    }
}
