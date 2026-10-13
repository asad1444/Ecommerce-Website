using System.ComponentModel.DataAnnotations;
using loginlogout.Areas.Identity.Data;

namespace loginlogout.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]

        public string City { get; set; }
        [Required]

        public string PostalCode { get; set; }
        [Required]

        public string Country { get; set; }
        [Required]

        public string Phone { get; set; }
        [Required]

        public string Email { get; set; }
       

        public string Name { get; set; }
        

        public decimal Price { get; set; }
        

        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Product> Products { get; set; }  
        public ICollection<CartItem> CartItems { get; set; }    
        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    }

}
