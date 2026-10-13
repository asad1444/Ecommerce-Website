using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using loginlogout.Models;
using System.Linq;

namespace loginlogout.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Search(string name, int price)
        {
            var find = _context.Products
                .Where(p =>
                    (!string.IsNullOrEmpty(name) && p.Name.Contains(name)) ||
                    (price > 0 && p.Price == price))
                .ToList();

            return View("~/Views/Shop/Search.cshtml", find);
        }
    }
}
