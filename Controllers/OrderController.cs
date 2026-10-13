using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Order()
        {
            var cartItems = await GetCartItemsAsync();
            if (cartItems.Count == 0)
            {
                TempData["StoreError"] = "Your cart is empty. Add an item before checking out.";
                return RedirectToAction("ShowCartItem", "ShowCartItem");
            }

            ViewBag.CartItems = cartItems;
            await SetCartCountAsync();
            var user = await _userManager.GetUserAsync(User);
            var nameParts = (user?.FullName ?? string.Empty)
                .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return View(new CheckoutInput
            {
                FirstName = nameParts.FirstOrDefault() ?? string.Empty,
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                Email = user?.Email ?? string.Empty,
                Phone = user?.PhoneNumber ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(CheckoutInput checkout)
        {
            var cartItems = await GetCartItemsAsync();
            if (cartItems.Count == 0)
            {
                TempData["StoreError"] = "Your cart is empty. Add an item before checking out.";
                return RedirectToAction("ShowCartItem", "ShowCartItem");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CartItems = cartItems;
                await SetCartCountAsync();
                return View(checkout);
            }

            var userId = _userManager.GetUserId(User)!;
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            foreach (var item in cartItems)
            {
                var product = await _context.Products.AsNoTracking()
                    .FirstOrDefaultAsync(candidate => candidate.ProductId == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    TempData["StoreError"] = $"{item.Product.Name} no longer has enough stock. Review your cart and try again.";
                    return RedirectToAction("ShowCartItem", "ShowCartItem");
                }

                var updated = await _context.Products
                    .Where(product => product.ProductId == item.ProductId && product.Stock >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.Stock, product => product.Stock - item.Quantity));

                if (updated != 1)
                {
                    await transaction.RollbackAsync();
                    TempData["StoreError"] = $"{item.Product.Name} no longer has enough stock. Review your cart and try again.";
                    return RedirectToAction("ShowCartItem", "ShowCartItem");
                }

                _context.InventoryAdjustments.Add(new InventoryAdjustment
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    PreviousStock = product.Stock,
                    NewStock = product.Stock - item.Quantity,
                    QuantityChange = -item.Quantity,
                    Reason = "Customer order checkout",
                    AdminName = "System",
                    ChangedAt = DateTime.UtcNow
                });
            }

            var total = cartItems.Sum(item => item.Product.Price * item.Quantity);
            var order = new Order
            {
                UserId = userId,
                FirstName = checkout.FirstName.Trim(),
                LastName = checkout.LastName.Trim(),
                Address = checkout.Address.Trim(),
                City = checkout.City.Trim(),
                PostalCode = checkout.PostalCode.Trim(),
                Country = checkout.Country.Trim(),
                Phone = checkout.Phone.Trim(),
                Email = checkout.Email.Trim(),
                Name = cartItems[0].Product.Name,
                Price = total,
                Quantity = cartItems.Sum(item => item.Quantity),
                Total = total,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = order.Status,
                AdminName = "Checkout",
                ChangedAt = order.CreatedAt
            });
            await _context.SaveChangesAsync();

            _context.OrderItems.AddRange(cartItems.Select(item => new OrderItem
            {
                OrderId = order.OrderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Product.Price
            }));

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction(nameof(Confirmation), new { id = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var order = await _context.Orders
                .AsNoTracking()
                .Include(item => item.User)
                .SingleOrDefaultAsync(item => item.OrderId == id && item.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItems = await _context.OrderItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.OrderId == order.OrderId)
                .ToListAsync();
            ViewBag.OrderItems = orderItems;
            await SetCartCountAsync();
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User)!;
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();

            await SetCartCountAsync();
            return View(orders);
        }

        private async Task SetCartCountAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            ViewBag.CountCartitem = await _context.CartItems
                .Where(item => item.UserId == userId && item.OrderId == null)
                .SumAsync(item => (int?)item.Quantity) ?? 0;
        }

        private Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = _userManager.GetUserId(User)!;
            return _context.CartItems
                .Include(item => item.Product)
                .Where(item => item.UserId == userId && item.OrderId == null)
                .OrderBy(item => item.CartItemId)
                .ToListAsync();
        }
    }
}
