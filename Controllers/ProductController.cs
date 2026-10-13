using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext context,IWebHostEnvironment env) {
            this._context = context;
            this._env = env;    
        }

        [HttpGet]
        public async Task<IActionResult> Addproduct()
        {
            await PopulateCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Addproduct(ProductVM prod)
        {
            prod.Name = prod.Name?.Trim() ?? string.Empty;
            if (prod.ProductPhoto == null)
            {
                ModelState.AddModelError(nameof(prod.ProductPhoto), "Choose a product image.");
            }
            ValidateImage(prod.ProductPhoto, nameof(prod.ProductPhoto));

            if (await _context.Products.AnyAsync(product => product.Name == prod.Name))
            {
                ModelState.AddModelError(nameof(prod.Name), "A product with this name already exists.");
            }

            if (!await _context.Category.AnyAsync(category => category.CategoryId == prod.CategoryId))
            {
                ModelState.AddModelError(nameof(prod.CategoryId), "Choose a valid category.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesAsync(prod.CategoryId);
                return View(prod);
            }

            var filename = await SaveImageAsync(prod.ProductPhoto!, "ProductImages");
            _context.Products.Add(new Product
            {
                Name = prod.Name,
                Description = prod.Description?.Trim() ?? string.Empty,
                Price = prod.Price,
                ProductImage = filename,
                Stock = prod.Stock,
                LongDescription = prod.LongDescription?.Trim() ?? string.Empty,
                CategoryId = prod.CategoryId
            });

            await _context.SaveChangesAsync();
            TempData["productadd"] = "Product added successfully.";
            return RedirectToAction(nameof(Showproducts));
        }

        public async Task<IActionResult> Showproducts()
        {
            var show = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .OrderBy(product => product.Name)
                .ToListAsync();
            return View(show);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var find = await _context.Products.FindAsync(id);
            if(find == null)
            {
                return NotFound();
            }
            ProductVM prod = new ProductVM()
            {
                ProductId = find.ProductId,
                Name = find.Name,
                Description = find.Description,
                Price = find.Price,
                LongDescription = find.LongDescription,
                CategoryId = find.CategoryId
            };
            ViewBag.CurrentImage = find.ProductImage;
            await PopulateCategoriesAsync(find.CategoryId);
           
            return View(prod);
        }

        [HttpPost]  
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductVM pro)
        {
            var find = await _context.Products.FindAsync(pro.ProductId);
            if(find == null)
            {
                return NotFound();
            }

            pro.Name = pro.Name?.Trim() ?? string.Empty;
            if (pro.ProductPhoto != null)
            {
                ValidateImage(pro.ProductPhoto, nameof(pro.ProductPhoto));
            }

            if (await _context.Products.AnyAsync(product => product.ProductId != pro.ProductId && product.Name == pro.Name))
            {
                ModelState.AddModelError(nameof(pro.Name), "A product with this name already exists.");
            }
            if (!await _context.Category.AnyAsync(category => category.CategoryId == pro.CategoryId))
            {
                ModelState.AddModelError(nameof(pro.CategoryId), "Choose a valid category.");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentImage = find.ProductImage;
                await PopulateCategoriesAsync(pro.CategoryId);
                return View(pro);
            }

            if (pro.ProductPhoto != null)
            {
                find.ProductImage = await SaveImageAsync(pro.ProductPhoto, "ProductImages");
            }

            find.Name = pro.Name;
            find.Description = pro.Description?.Trim() ?? string.Empty;
            find.Price = pro.Price;
            find.LongDescription = pro.LongDescription?.Trim() ?? string.Empty;
            find.CategoryId = pro.CategoryId;

            await _context.SaveChangesAsync();
            TempData["productupdate"] = "Product updated successfully.";
            return RedirectToAction(nameof(Showproducts));
        }

        [HttpGet]
        public IActionResult DeleteProduct(int id)
        {
            var delete = _context.Products.Include(c=>c.Category).FirstOrDefault(p=>p.ProductId == id);
            if(delete == null)
            {
                return NotFound();
            }
            return View(delete);
        }
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProducts(int id)
        {
            var remove = await _context.Products.FirstOrDefaultAsync(product => product.ProductId == id);
            if (remove != null)
            {
                if (await _context.OrderItems.AnyAsync(item => item.ProductId == id))
                {
                    TempData["producterror"] = "This product is part of an order and cannot be deleted. Set its stock to zero to stop selling it.";
                    return RedirectToAction(nameof(Showproducts));
                }

                _context.Products.Remove(remove);
                await _context.SaveChangesAsync();
                TempData["Removeproduct"] = "Product Deleted Successfully";
                return RedirectToAction(nameof(Showproducts));
            }
            return NotFound();
        }

        private async Task PopulateCategoriesAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Category.AsNoTracking().OrderBy(category => category.Name).ToListAsync();
            ViewBag.Category = new SelectList(categories, "CategoryId", "Name", selectedCategoryId);
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
            var folder = Path.Combine(_env.WebRootPath, folderName);
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, filename);

            await using var stream = new FileStream(path, FileMode.CreateNew);
            await image.CopyToAsync(stream);
            return filename;
        }
    }
}
