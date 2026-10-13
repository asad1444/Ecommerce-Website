using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using loginlogout.Models; 
namespace loginlogout.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; }

    public ICollection<Order> Orders { get; set; }
    public ICollection<CartItem> CartItems { get; set; }
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

}
