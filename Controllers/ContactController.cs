using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "User")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public ContactController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            var userId = userManager.GetUserId(User);
            var cartitems = await context.CartItems
                .AsNoTracking()
                .Include(item => item.Product)
                .Where(item => item.UserId == userId && item.OrderId == null)
                .ToListAsync();
            ViewBag.CartItems = cartitems;
            ViewBag.CountCartitem = context.CartItems
                .Where(item => item.UserId == userId && item.OrderId == null)
                .Sum(item => (int?)item.Quantity) ?? 0;
            return View(new Contact());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(Contact contact)
        {
            contact.Email = contact.Email?.Trim() ?? string.Empty;
            contact.Message = contact.Message?.Trim() ?? string.Empty;
            if (contact.Email.Length > 254)
            {
                ModelState.AddModelError(nameof(contact.Email), "Email addresses must be 254 characters or fewer.");
            }
            if (contact.Message.Length > 4000)
            {
                ModelState.AddModelError(nameof(contact.Message), "Messages must be 4,000 characters or fewer.");
            }

            if (!ModelState.IsValid)
            {
                var userId = userManager.GetUserId(User)!;
                ViewBag.CartItems = await context.CartItems
                    .AsNoTracking()
                    .Include(item => item.Product)
                    .Where(item => item.UserId == userId && item.OrderId == null)
                    .ToListAsync();
                ViewBag.CountCartitem = await context.CartItems
                    .Where(item => item.UserId == userId && item.OrderId == null)
                    .SumAsync(item => (int?)item.Quantity) ?? 0;
                return View(contact);
            }

            contact.SubmittedAt = DateTime.UtcNow;
            context.Contacts.Add(contact);
            await context.SaveChangesAsync();

            TempData["StoreMessage"] = "Thanks for reaching out. Your message has been sent.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Search(string searchProduct)
        {
            var search = context.Products.Where(p => p.Name.Contains(searchProduct));
            return View(search);
        }
    }
}
