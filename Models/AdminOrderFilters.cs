namespace loginlogout.Models
{
    public class AdminOrderFilters
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();
    }
}
