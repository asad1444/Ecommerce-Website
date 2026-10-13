namespace loginlogout.Models
{
    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = null!;
        public IReadOnlyList<OrderItem> Items { get; set; } = Array.Empty<OrderItem>();
        public IReadOnlyList<OrderStatusHistory> StatusHistory { get; set; } = Array.Empty<OrderStatusHistory>();
    }
}
