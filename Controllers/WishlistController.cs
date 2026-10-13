using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;
            var items = await _context.WishlistItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => item.AddedAt)
                .ToListAsync();

            ViewBag.CountCartitem = await _context.CartItems
                .Where(item => item.UserId == userId && item.OrderId == null)
                .SumAsync(item => (int?)item.Quantity) ?? 0;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, string? returnUrl = null)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User)!;
            var alreadySaved = await _context.WishlistItems
                .AnyAsync(item => item.UserId == userId && item.ProductId == productId);
            if (alreadySaved)
            {
                TempData["StoreMessage"] = "This item is already in your wishlist.";
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId
                });
                await _context.SaveChangesAsync();
                TempData["StoreMessage"] = $"{product.Name} was added to your wishlist.";
            }

            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _context.WishlistItems
                .SingleOrDefaultAsync(entry => entry.WishlistItemId == id && entry.UserId == userId);

            if (item == null)
            {
                return NotFound();
            }

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["StoreMessage"] = "The item was removed from your wishlist.";
            return RedirectToAction(nameof(Index));
        }
    }
}
