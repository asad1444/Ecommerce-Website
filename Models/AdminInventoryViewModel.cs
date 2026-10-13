namespace loginlogout.Models
{
    public class AdminInventoryViewModel
    {
        public IReadOnlyList<Product> Products { get; set; } = Array.Empty<Product>();
        public IReadOnlyList<InventoryAdjustment> RecentAdjustments { get; set; } = Array.Empty<InventoryAdjustment>();
    }
}
