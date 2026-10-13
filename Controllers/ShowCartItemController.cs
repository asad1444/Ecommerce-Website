using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]
    public class ShowCartItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShowCartItemController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> ShowCartItem()
        {
            var userId = _userManager.GetUserId(User)!;
            var cartItems = await _context.CartItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.UserId == userId && item.OrderId == null)
                .OrderBy(item => item.CartItemId)
                .ToListAsync();

            ViewBag.CountCartitem = cartItems.Sum(item => item.Quantity);
            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1, string? returnUrl = null)
        {
            if (quantity < 1)
            {
                TempData["StoreError"] = "Choose a quantity of at least one.";
                return RedirectToLocal(returnUrl);
            }

            var userId = _userManager.GetUserId(User)!;
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItems
                .SingleOrDefaultAsync(item => item.UserId == userId && item.ProductId == id && item.OrderId == null);
            var currentQuantity = cartItem?.Quantity ?? 0;
            if (product.Stock <= 0 || quantity > product.Stock - currentQuantity)
            {
                TempData["StoreError"] = product.Stock <= 0
                    ? "This item is currently out of stock."
                    : $"Only {product.Stock} item(s) are available.";
                return RedirectToLocal(returnUrl);
            }

            if (cartItem == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = id,
                    Quantity = quantity
                });
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            TempData["StoreMessage"] = $"{product.Name} was added to your cart.";
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var userId = _userManager.GetUserId(User)!;
            var cartItem = await _context.CartItems
                .Include(item => item.Product)
                .SingleOrDefaultAsync(item => item.CartItemId == id && item.UserId == userId && item.OrderId == null);

            if (cartItem == null)
            {
                return NotFound();
            }

            if (quantity < 1 || quantity > cartItem.Product.Stock)
            {
                TempData["StoreError"] = quantity < 1
                    ? "Cart quantity must be at least one."
                    : $"Only {cartItem.Product.Stock} item(s) are available.";
                return RedirectToAction(nameof(ShowCartItem));
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();
            TempData["StoreMessage"] = "Your cart was updated.";
            return RedirectToAction(nameof(ShowCartItem));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var cartItem = await _context.CartItems
                .SingleOrDefaultAsync(item => item.CartItemId == id && item.UserId == userId && item.OrderId == null);

            if (cartItem == null)
            {
                return NotFound();
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            TempData["StoreMessage"] = "The item was removed from your cart.";
            return RedirectToAction(nameof(ShowCartItem));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(ShowCartItem));
        }
    }
}
