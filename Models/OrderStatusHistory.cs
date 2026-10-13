using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        [StringLength(32)]
        public string Status { get; set; } = string.Empty;
        [StringLength(256)]
        public string AdminName { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
