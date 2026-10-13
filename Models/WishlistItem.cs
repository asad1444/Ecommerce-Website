using loginlogout.Areas.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Models;

[Index(nameof(UserId), nameof(ProductId), IsUnique = true)]
public class WishlistItem
{
    public int WishlistItemId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
