using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminsearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminsearchController(ApplicationDbContext context)
        {
            this._context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SearchProduct(string? name)
        {
            var products = _context.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
            {
                products = products.Where(product => product.Name.Contains(name.Trim()));
            }

            ViewBag.SearchName = name;
            return View(await products.OrderBy(product => product.Name).ToListAsync());
        }

        [HttpGet]
        public IActionResult DeleteProduct(int id)
        {
            return RedirectToAction("DeleteProduct", "Product", new { id });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            return RedirectToAction("Edit", "Product", new { id });
        }

    }
}
