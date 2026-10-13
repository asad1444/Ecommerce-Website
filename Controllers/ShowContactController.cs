using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShowContactController : Controller
    {
        private readonly ApplicationDbContext context;

        public ShowContactController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> ShowContact()
        {
            var showcontact = await context.Contacts
                .AsNoTracking()
                .OrderByDescending(contact => contact.SubmittedAt)
                .ToListAsync();
            return View(showcontact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }

            context.Contacts.Remove(contact);
            await context.SaveChangesAsync();
            TempData["deletecontact"] = "Message deleted.";
            return RedirectToAction(nameof(ShowContact));
        }
    }
}
