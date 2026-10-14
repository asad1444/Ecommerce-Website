using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using loginlogout.Services;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]

    public class AdminController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;  

        public AdminController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Dashboard(string? period = "weekly")
        {
            var today = DateTime.Today;
            var selectedPeriod = period?.Trim().ToLowerInvariant();
            if (selectedPeriod is not ("daily" or "weekly" or "yearly"))
            {
                TempData["error"] = "Choose Daily, Weekly, or Yearly sales trend.";
                selectedPeriod = "weekly";
            }

            var start = selectedPeriod switch
            {
                "daily" => today,
                "yearly" => new DateTime(today.Year, today.Month, 1).AddMonths(-11),
                _ => today.AddDays(-6)
            };
            var end = today;
            var startUtc = DateTime.SpecifyKind(start, DateTimeKind.Local).ToUniversalTime();
            var endExclusiveUtc = DateTime.SpecifyKind(end.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            var periodOrders = context.Orders.AsNoTracking()
                .Where(order => order.CreatedAt >= startUtc && order.CreatedAt < endExclusiveUtc);
            var periodOrderIds = periodOrders.Select(order => order.OrderId);
            var orderRows = await periodOrders
                .Select(order => new { order.CreatedAt, order.Total })
                .ToListAsync();
            var localOrders = orderRows.Select(order => new
            {
                LocalDate = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc).ToLocalTime(),
                order.Total
            });
            var salesTrend = selectedPeriod switch
            {
                "daily" => Enumerable.Range(0, 24)
                    .Select(hour =>
                    {
                        var hourStart = today.AddHours(hour);
                        var matchingOrders = localOrders.Where(order => order.LocalDate.Date == today && order.LocalDate.Hour == hour).ToList();
                        return new AdminSalesTrendPointViewModel
                        {
                            Date = hourStart,
                            Label = hourStart.ToString("h tt"),
                            Orders = matchingOrders.Count,
                            Revenue = matchingOrders.Sum(order => order.Total)
                        };
                    })
                    .ToList(),
                "yearly" => Enumerable.Range(0, 12)
                    .Select(offset =>
                    {
                        var month = start.AddMonths(offset);
                        var matchingOrders = localOrders.Where(order =>
                            order.LocalDate.Year == month.Year && order.LocalDate.Month == month.Month).ToList();
                        return new AdminSalesTrendPointViewModel
                        {
                            Date = month,
                            Label = month.ToString("MMM"),
                            Orders = matchingOrders.Count,
                            Revenue = matchingOrders.Sum(order => order.Total)
                        };
                    })
                    .ToList(),
                _ => Enumerable.Range(0, 7)
                    .Select(offset =>
                    {
                        var date = start.AddDays(offset);
                        var matchingOrders = localOrders.Where(order => order.LocalDate.Date == date).ToList();
                        return new AdminSalesTrendPointViewModel
                        {
                            Date = date,
                            Label = date.ToString("ddd"),
                            Orders = matchingOrders.Count,
                            Revenue = matchingOrders.Sum(order => order.Total)
                        };
                    })
                    .ToList()
            };
            var dashboard = new AdminDashboardViewModel
            {
                Period = selectedPeriod,
                FromDate = start,
                ToDate = end,
                TotalCustomers = (await userManager.GetUsersInRoleAsync("User")).Count,
                TotalOrders = orderRows.Count,
                PendingOrders = await periodOrders.CountAsync(order => order.Status == "pending"),
                LowStockProducts = await context.Products.CountAsync(product => product.Stock <= 5),
                Revenue = orderRows.Sum(order => order.Total),
                RecentOrders = await context.Orders.AsNoTracking()
                    .OrderByDescending(order => order.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                SalesTrend = salesTrend
            };
            var topProductRows = await context.OrderItems.AsNoTracking()
                    .Where(item => periodOrderIds.Contains(item.OrderId))
                    .Select(item => new
                    {
                        item.ProductId,
                        Name = item.Product.Name,
                        item.Quantity,
                        item.Price
                    })
                    .ToListAsync();
            dashboard.TopProducts = topProductRows
                .GroupBy(item => new { item.ProductId, item.Name })
                .Select(group => new AdminTopProductViewModel
                {
                    Name = group.Key.Name,
                    UnitsSold = group.Sum(item => item.Quantity),
                    Revenue = group.Sum(item => item.Price * item.Quantity)
                })
                .OrderByDescending(item => item.UnitsSold)
                .Take(5)
                .ToList();
            ViewBag.OrderStatuses = new[] { "pending", "processing", "shipped", "delivered" };
            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> Orders(AdminOrderFilters filters)
        {
            var orders = context.Orders.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                var search = filters.Search.Trim();
                if (int.TryParse(search.TrimStart('#'), out var orderId))
                {
                    orders = orders.Where(order => order.OrderId == orderId);
                }
                else
                {
                    orders = orders.Where(order =>
                        order.FirstName.Contains(search) ||
                        order.LastName.Contains(search) ||
                        order.Email.Contains(search));
                }
            }

            if (!string.IsNullOrWhiteSpace(filters.Status))
            {
                orders = orders.Where(order => order.Status == filters.Status);
            }
            if (filters.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(filters.FromDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
                orders = orders.Where(order => order.CreatedAt >= fromUtc);
            }
            if (filters.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(filters.ToDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
                orders = orders.Where(order => order.CreatedAt < toUtc);
            }
            if (filters.FromDate.HasValue && filters.ToDate.HasValue &&
                (filters.ToDate.Value.Date < filters.FromDate.Value.Date ||
                 (filters.ToDate.Value.Date - filters.FromDate.Value.Date).TotalDays > 365))
            {
                ModelState.AddModelError(string.Empty, "Choose a valid order date range of up to 366 days.");
                filters.Orders = Array.Empty<Order>();
                return View(filters);
            }

            filters.Orders = await orders.OrderByDescending(order => order.CreatedAt).Take(500).ToListAsync();
            return View(filters);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await context.Orders.AsNoTracking()
                .Include(item => item.User)
                .Include(item => item.StatusHistory)
                .FirstOrDefaultAsync(item => item.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var items = await context.OrderItems.AsNoTracking()
                .Where(item => item.OrderId == id)
                .Include(item => item.Product)
                .ToListAsync();
            ViewBag.OrderStatuses = new[] { "pending", "processing", "shipped", "delivered" };
            return View(new OrderDetailsViewModel
            {
                Order = order,
                Items = items,
                StatusHistory = order.StatusHistory.OrderByDescending(history => history.ChangedAt).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(item => item.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            var items = await context.OrderItems.AsNoTracking()
                .Where(item => item.OrderId == id)
                .Include(item => item.Product)
                .ToListAsync();
            var invoice = new OrderDetailsViewModel { Order = order, Items = items };
            var pdf = InvoicePdfGenerator.Generate(invoice);
            return File(pdf, "application/pdf", $"invoice-order-{order.OrderId}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var model = new AdminInventoryViewModel
            {
                Products = await context.Products.AsNoTracking()
                    .OrderBy(product => product.Stock)
                    .ThenBy(product => product.Name)
                    .ToListAsync(),
                RecentAdjustments = await context.InventoryAdjustments.AsNoTracking()
                    .OrderByDescending(adjustment => adjustment.ChangedAt)
                    .Take(100)
                    .ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustInventory(InventoryAdjustmentInput input)
        {
            var isAdd = string.Equals(input.AdjustmentType, "add", StringComparison.OrdinalIgnoreCase);
            var isRemove = string.Equals(input.AdjustmentType, "remove", StringComparison.OrdinalIgnoreCase);
            if (!isAdd && !isRemove)
            {
                ModelState.AddModelError(nameof(input.AdjustmentType), "Choose add or remove stock.");
            }
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Enter a valid quantity, adjustment type, and reason.";
                return RedirectToAction(nameof(Inventory));
            }

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var product = await context.Products.FirstOrDefaultAsync(item => item.ProductId == input.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            if (isRemove && product.Stock < input.Quantity)
            {
                TempData["error"] = $"Only {product.Stock} unit(s) are currently in stock.";
                return RedirectToAction(nameof(Inventory));
            }
            if (isAdd && product.Stock > int.MaxValue - input.Quantity)
            {
                TempData["error"] = "This adjustment would exceed the maximum stock value.";
                return RedirectToAction(nameof(Inventory));
            }

            var previousStock = product.Stock;
            var signedQuantity = isAdd ? input.Quantity : -input.Quantity;
            product.Stock += signedQuantity;
            context.InventoryAdjustments.Add(new InventoryAdjustment
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                PreviousStock = previousStock,
                NewStock = product.Stock,
                QuantityChange = signedQuantity,
                Reason = input.Reason.Trim(),
                AdminName = User.Identity?.Name ?? "Admin",
                ChangedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["success"] = $"{product.Name} stock updated successfully.";
            return RedirectToAction(nameof(Inventory));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var allowedStatuses = new[] { "pending", "processing", "shipped", "delivered" };
            if (!allowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                TempData["error"] = "Choose a valid order status.";
                return RedirectToAction(nameof(Dashboard));
            }

            var order = await context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var normalizedStatus = status.ToLowerInvariant();
            if (!string.Equals(order.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                order.Status = normalizedStatus;
                context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    Status = normalizedStatus,
                    AdminName = User.Identity?.Name ?? "Admin",
                    ChangedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();
            TempData["success"] = $"Order #{order.OrderId} status updated.";
            return RedirectToAction(nameof(OrderDetails), new { id = order.OrderId });
        }

        [HttpGet]
        public async Task<IActionResult> EditOrder(int id)
        {
            var findorder = await context.Orders.FindAsync(id);
            if (findorder == null)
            {
                return NotFound();
            }
            ViewBag.UserId = new SelectList(await context.Users.ToListAsync(), "Id", "Email");

            return View(findorder);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOrder(Order order)
        {
            var editorder = await context.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
            if (editorder == null)
            {
                return NotFound();
            }
            var allowedStatuses = new[] { "pending", "processing", "shipped", "delivered" };
            if (!allowedStatuses.Contains(order.Status, StringComparer.OrdinalIgnoreCase))
            {
                TempData["error"] = "Choose a valid order status.";
                return RedirectToAction(nameof(OrderDetails), new { id = order.OrderId });
            }
            editorder.UserId = order.UserId;
            editorder.FirstName = order.FirstName;
            editorder.LastName = order.LastName;
            editorder.Address = order.Address;
            editorder.City = order.City;
            editorder.Email = order.Email;
            var normalizedStatus = order.Status.ToLowerInvariant();
            if (!string.Equals(editorder.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                editorder.Status = normalizedStatus;
                context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = editorder.OrderId,
                    Status = normalizedStatus,
                    AdminName = User.Identity?.Name ?? "Admin",
                    ChangedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderDetails), new { id = editorder.OrderId });
        }

        [HttpGet]
        public IActionResult DeleteOrder(int id)
        {
            var finddeleteorder = context.Orders.Include(u=>u.User).FirstOrDefault(o=>o.OrderId == id);
            if(finddeleteorder == null)
            {
                return NotFound();
            }
            return View(finddeleteorder); 
        }

        [HttpPost, ActionName("DeleteOrder")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOrders(int id)
        {
            var deleteOrder = context.Orders.Find(id);
            if(deleteOrder == null)
            {
                return NotFound();
            }
            context.Orders.Remove(deleteOrder);
            context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        public IActionResult Search(string Searchstatus)
        {
            var searchResults = context.Orders
                .Where(o => o.Name.Contains(Searchstatus) && o.Status.Contains(Searchstatus))
                .ToList();
            return View(searchResults);
        }

    }
}
