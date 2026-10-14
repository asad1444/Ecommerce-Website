using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
public AboutController(ApplicationDbContext context,IWebHostEnvironment env)
        {
_context = context;
            _env = env;
        }
        [HttpGet]
        public IActionResult AddAbout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAbout(AboutVM about)
        {
            ValidateContent(about);
            if (about.UpperPhoto == null)
            {
                ModelState.AddModelError(nameof(about.UpperPhoto), "Choose an image for the About section.");
            }
            ValidateImage(about.UpperPhoto);
            if (!ModelState.IsValid)
            {
                return View(about);
            }

            var filename = await SaveImageAsync(about.UpperPhoto!);
            _context.About.Add(new About
            {
                UpperImage = filename,
                Uppertitle = about.Uppertitle.Trim(),
                uppercontent = about.uppercontent.Trim()
            });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowAbout));
        }

        public async Task<IActionResult> ShowAbout()
        {
            var show = await _context.About.AsNoTracking().ToListAsync();
            return View(show);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAbout(int id)
        {
            var about = await _context.About.FindAsync(id);
            return about == null ? NotFound() : View(about);
        }

        [HttpPost,ActionName("DeleteAbout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAbouts(int id)
        {
            var about = await _context.About.FindAsync(id);
            if (about == null)
            {
                return NotFound();
            }

            _context.About.Remove(about);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowAbout));
        }

        [HttpGet]
        public async Task<IActionResult> EditAbout(int id)
        {
            var about = await _context.About.FindAsync(id);
            if (about == null)
            {
                return NotFound();
            }

            ViewBag.BannerImage = about.UpperImage;
            return View(new AboutVM
            {
                AboutId = about.AboutId,
                Uppertitle = about.Uppertitle,
                uppercontent = about.uppercontent
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAbout(AboutVM about)
        {
            var existing = await _context.About.FindAsync(about.AboutId);
            if (existing == null)
            {
                return NotFound();
            }

            ValidateContent(about);
            ValidateImage(about.UpperPhoto);
            if (!ModelState.IsValid)
            {
                ViewBag.BannerImage = existing.UpperImage;
                return View(about);
            }

            if (about.UpperPhoto != null)
            {
                existing.UpperImage = await SaveImageAsync(about.UpperPhoto);
            }

            existing.Uppertitle = about.Uppertitle.Trim();
            existing.uppercontent = about.uppercontent.Trim();
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ShowAbout));
        }

        private void ValidateContent(AboutVM about)
        {
            if (string.IsNullOrWhiteSpace(about.Uppertitle))
            {
                ModelState.AddModelError(nameof(about.Uppertitle), "Enter a title.");
            }

            if (string.IsNullOrWhiteSpace(about.uppercontent))
            {
                ModelState.AddModelError(nameof(about.uppercontent), "Enter the About content.");
            }
        }

        private void ValidateImage(IFormFile? image)
        {
            if (image == null)
            {
                return;
            }

            var extension = Path.GetExtension(image.FileName);
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(AboutVM.UpperPhoto), "Use a JPG, PNG, or WebP image.");
            }

            if (image.Length == 0 || image.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(AboutVM.UpperPhoto), "Images must be between 1 byte and 5 MB.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var folder = Path.Combine(_env.WebRootPath, "AboutImages");
            Directory.CreateDirectory(folder);
            var filename = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
            var path = Path.Combine(folder, filename);

            await using var stream = new FileStream(path, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return filename;
        }
    }
}
