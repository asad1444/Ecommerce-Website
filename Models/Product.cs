namespace loginlogout.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ProductImage { get; set; }
        public int Stock { get; set; }

        public string LongDescription { get; set; }

        // Foreign Key
        public int CategoryId { get; set; }
        public Category Category { get; set; }



        // Navigation
        public ICollection<CartItem> CartItems { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
