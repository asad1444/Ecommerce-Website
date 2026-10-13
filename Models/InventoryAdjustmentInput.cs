using System.ComponentModel.DataAnnotations;

namespace loginlogout.Models
{
    public class InventoryAdjustmentInput
    {
        public int ProductId { get; set; }

        [Required]
        public string AdjustmentType { get; set; } = "add";

        [Range(1, 1000000)]
        public int Quantity { get; set; }

        [Required, StringLength(200)]
        public string Reason { get; set; } = string.Empty;
    }
}
