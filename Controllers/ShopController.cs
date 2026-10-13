using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]

    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;
       public ShopController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager)
        {
            this._context = context;
            this._usermanager = usermanager;
        }
        [HttpGet]
        public async Task<IActionResult> Shop(int? categoryId, string? searchProduct, string? sortBy)
        {
            var userId = _usermanager.GetUserId(User)!;
            var products = _context.Products.AsNoTracking().AsQueryable();
            if (categoryId.HasValue)
            {
                products = products.Where(product => product.CategoryId == categoryId.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchProduct))
            {
                products = products.Where(product => product.Name.Contains(searchProduct.Trim()));
            }

            products = sortBy switch
            {
                "price-low" => products.OrderBy(product => product.Price),
                "price-high" => products.OrderByDescending(product => product.Price),
                "name" => products.OrderBy(product => product.Name),
                _ => products.OrderBy(product => product.ProductId)
            };

            ViewBag.CartItems = await GetCartItemsAsync(userId);
            ViewBag.Category = await _context.Category.AsNoTracking().ToListAsync();
            ViewBag.CountCartitem = await _context.CartItems
                .Where(item => item.UserId == userId && item.OrderId == null)
                .SumAsync(item => (int?)item.Quantity) ?? 0;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchProduct = searchProduct;
            ViewBag.SortBy = sortBy;
            return View(await products.ToListAsync());
        }

        public IActionResult ByCategory(int id)
        {
            return RedirectToAction(nameof(Shop), new { categoryId = id });
        }

        [HttpGet]
        public Task<IActionResult> Search(string? searchProduct)
        {
            return SearchResultsAsync(searchProduct);
        }

        private async Task<IActionResult> SearchResultsAsync(string? searchProduct)
        {
            var userId = _usermanager.GetUserId(User)!;
            var products = _context.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(searchProduct))
            {
                products = products.Where(product => product.Name.Contains(searchProduct.Trim()));
            }

            ViewBag.CartItems = await GetCartItemsAsync(userId);
            ViewBag.Category = await _context.Category.AsNoTracking().ToListAsync();
            ViewBag.CountCartitem = await _context.CartItems
                .Where(item => item.UserId == userId && item.OrderId == null)
                .SumAsync(item => (int?)item.Quantity) ?? 0;
            ViewBag.SearchProduct = searchProduct;
            return View("Shop", await products.OrderBy(product => product.ProductId).ToListAsync());
        }

        private Task<List<CartItem>> GetCartItemsAsync(string userId)
        {
            return _context.CartItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.UserId == userId && item.OrderId == null)
                .ToListAsync();
        }
    }
}
