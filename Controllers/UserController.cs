using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context; 
        private readonly UserManager<ApplicationUser> _userManager; 
public UserController(ApplicationDbContext context,UserManager<ApplicationUser> userManager) { 
            this._context = context;
            this._userManager = userManager;
        
        }

        [AllowAnonymous]
        public IActionResult Profile()
        {
            var products = _context.Products.AsNoTracking().Take(8).ToList();
            var categories = _context.Category.AsNoTracking().ToList();
            var userId = _userManager.GetUserId(User);
            var cartItems = userId == null
                ? new List<loginlogout.Models.CartItem>()
                : _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId && c.OrderId == null)
                    .ToList();

            ViewBag.CartItems = cartItems;
            ViewBag.Category = categories;
            ViewBag.Cartcount = cartItems.Sum(item => item.Quantity);

         
            return View(products); 
        }

           public IActionResult ByCategory(int id) {
               return RedirectToAction("Shop", "Shop", new { categoryId = id });
           }

           [HttpGet]
           public IActionResult Search(string? searchProduct)
           {
               return RedirectToAction("Search", "Shop", new { searchProduct });
           }

      
    }
}
