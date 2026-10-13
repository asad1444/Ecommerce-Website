using loginlogout.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Areas.Identity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Category> Category { get; set; }   
        public DbSet<Product> Products { get; set; }    

        public DbSet<CartItem> CartItems { get; set; }  
        public DbSet<Order> Orders { get; set; }    
        public DbSet<OrderItem> OrderItems { get; set; }    
        public DbSet<Contact> Contacts { get; set; }    
        public DbSet<Blog> Blogs { get; set; }

        public DbSet<About> About { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
        public DbSet<InventoryAdjustment> InventoryAdjustments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Product>().Property(product => product.Price).HasPrecision(18, 2);
            builder.Entity<Order>().Property(order => order.Price).HasPrecision(18, 2);
            builder.Entity<Order>().Property(order => order.Total).HasPrecision(18, 2);
            builder.Entity<OrderItem>().Property(item => item.Price).HasPrecision(18, 2);
            builder.Entity<OrderStatusHistory>()
                .HasOne(history => history.Order)
                .WithMany(order => order.StatusHistory)
                .HasForeignKey(history => history.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
