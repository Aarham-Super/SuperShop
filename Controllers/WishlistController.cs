using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using SuperShop.Models;

namespace SuperShop.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        public WishlistController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await _context.WishlistItems.Where(item => item.UserId == userId)
                .Include(item => item.Product!).ThenInclude(product => product.Images)
                .Include(item => item.Product!).ThenInclude(product => product.Category)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var exists = await _context.Products.AnyAsync(product => product.Id == productId && product.IsActive && product.Category != null && product.Category.IsActive);
            if (!exists) { TempData["ErrorMessage"] = "That product is no longer available."; return RedirectToAction("Index", "Products"); }
            if (!await _context.WishlistItems.AnyAsync(item => item.UserId == userId && item.ProductId == productId))
            {
                _context.WishlistItems.Add(new WishlistItem { UserId = userId!, ProductId = productId, AddedAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product saved to your wishlist.";
            }
            else TempData["SuccessMessage"] = "That product is already in your wishlist.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var item = await _context.WishlistItems.FirstOrDefaultAsync(saved => saved.Id == id && saved.UserId == userId);
            if (item != null) { _context.WishlistItems.Remove(item); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Product removed from your wishlist."; }
            return RedirectToAction(nameof(Index));
        }
    }
}
