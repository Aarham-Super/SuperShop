using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;

namespace SuperShop.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.Categories
                .Where(c => c.Id == id && c.IsActive)
                .Include(c => c.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Images)
                .Include(c => c.Products.Where(p => p.IsActive))
                    .ThenInclude(p => p.Inventory)
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
    }
}
