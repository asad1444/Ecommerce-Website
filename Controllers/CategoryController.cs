using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoryController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult AddCategory() => View(new CategoryVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(CategoryVM category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            if (category.CategoryPhoto == null)
            {
                ModelState.AddModelError(nameof(category.CategoryPhoto), "Choose a category image.");
            }
            ValidateImage(category.CategoryPhoto, nameof(category.CategoryPhoto));

            if (await _context.Category.AnyAsync(item => item.Name == category.Name))
            {
                ModelState.AddModelError(nameof(category.Name), "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var filename = await SaveImageAsync(category.CategoryPhoto!, "CategoryImages");
            _context.Category.Add(new Category
            {
                Name = category.Name,
                CategoryImage = filename
            });
            await _context.SaveChangesAsync();
            TempData["addcategory"] = "Category added successfully.";
            return RedirectToAction(nameof(ShowCategory));
        }

        [HttpGet]
        public async Task<IActionResult> ShowCategory()
        {
            var categories = await _context.Category
                .AsNoTracking()
                .OrderBy(item => item.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _context.Category.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            ViewBag.CurrentImage = existing.CategoryImage;
            return View(new CategoryVM
            {
                CategoryId = existing.CategoryId,
                Name = existing.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryVM category)
        {
            var existing = await _context.Category.FindAsync(category.CategoryId);
            if (existing == null)
            {
                return NotFound();
            }

            category.Name = category.Name?.Trim() ?? string.Empty;
            if (category.CategoryPhoto != null)
            {
                ValidateImage(category.CategoryPhoto, nameof(category.CategoryPhoto));
            }
            if (await _context.Category.AnyAsync(item =>
                    item.CategoryId != category.CategoryId && item.Name == category.Name))
            {
                ModelState.AddModelError(nameof(category.Name), "A category with this name already exists.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentImage = existing.CategoryImage;
                return View(category);
            }

            if (category.CategoryPhoto != null)
            {
                existing.CategoryImage = await SaveImageAsync(category.CategoryPhoto, "CategoryImages");
            }
            existing.Name = category.Name;
            await _context.SaveChangesAsync();
            TempData["updatecategory"] = "Category updated successfully.";
            return RedirectToAction(nameof(ShowCategory));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Category.FindAsync(id);
            return category == null ? NotFound() : View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            if (await _context.Products.AnyAsync(product => product.CategoryId == id))
            {
                TempData["categoryerror"] = "This category still has products. Move or remove those products before deleting it.";
                return RedirectToAction(nameof(ShowCategory));
            }

            _context.Category.Remove(category);
            await _context.SaveChangesAsync();
            TempData["removecategory"] = "Category Deleted Successfully";
            return RedirectToAction(nameof(ShowCategory));
        }

        private void ValidateImage(IFormFile? image, string modelKey)
        {
            if (image == null)
            {
                return;
            }

            var extension = Path.GetExtension(image.FileName);
            var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!permittedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(modelKey, "Use a JPG, PNG, or WebP image.");
            }
            if (image.Length == 0 || image.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(modelKey, "Image files must be between 1 byte and 5 MB.");
            }
        }

        private async Task<string> SaveImageAsync(IFormFile image, string folderName)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var filename = $"{Guid.NewGuid():N}{extension}";
            var folder = Path.Combine(_environment.WebRootPath, folderName);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, filename);

            await using var stream = new FileStream(path, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return filename;
        }
    }
}
