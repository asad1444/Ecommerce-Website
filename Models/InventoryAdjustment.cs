using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class InventoryAdjustment
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [StringLength(120)]
        public string ProductName { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public int QuantityChange { get; set; }
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;
        [StringLength(256)]
        public string AdminName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
