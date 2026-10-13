using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    public class PagesController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;  

        public PagesController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public IActionResult WebAbout()
        {

            var userId = userManager.GetUserId(User);
            if(userId == null)
            {
                return NotFound();
            }
            var cartitems = context.CartItems.Include(p => p.Product).Where(c=>c.UserId == userId).ToList();
            ViewBag.CartItems = cartitems;
            ViewBag.CountCartitem = context.CartItems
                           .Where(c => c.UserId == userId)
                           .Count(); var show = context.About.ToList();
            return View(show);
        }

        [HttpGet]
        public IActionResult Search(string searchProduct)
        {
            var results = context.Products
                .Where(p => p.Name.Contains(searchProduct))
                .ToList();

            return View(results);
        }
    }
}
