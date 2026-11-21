using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SuperShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? q, int? categoryId, decimal? minPrice, decimal? maxPrice, string? color, string? size)
        {
            var query = _context.Products.Where(p => p.IsActive && p.Category != null && p.Category.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q) || (p.ShortDescription != null && p.ShortDescription.Contains(q)) || (p.Description != null && p.Description.Contains(q)));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (minPrice.HasValue) query = query.Where(p => (p.SalePrice ?? p.Price) >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => (p.SalePrice ?? p.Price) <= maxPrice.Value);
            if (!string.IsNullOrWhiteSpace(color)) query = query.Where(p => p.Colors.Any(c => c.Name == color));
            if (!string.IsNullOrWhiteSpace(size)) query = query.Where(p => p.Sizes.Any(s => s.Name == size));

            var products = await query.Include(p => p.Category).Include(p => p.Images).Include(p => p.Inventory)
                .OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name).ToListAsync();
            var categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.Search = q;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Color = color;
            ViewBag.Size = size;
            var availableColors = await _context.ProductColors.Select(c => c.Name).Distinct().OrderBy(c => c).ToListAsync();
            var availableSizes = await _context.ProductSizes.Select(s => s.Name).Distinct().OrderBy(s => s).ToListAsync();
            ViewBag.ColorOptions = new SelectList(availableColors, color);
            ViewBag.SizeOptions = new SelectList(availableSizes, size);
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Where(p => p.Id == id && p.IsActive && p.Category != null && p.Category.IsActive)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Inventory)
                .Include(p => p.Sizes)
                .Include(p => p.Colors)
                .Include(p => p.Reviews.Where(review => review.IsApproved))
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}
