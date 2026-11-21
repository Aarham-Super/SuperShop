using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using SuperShop.Models;

namespace SuperShop.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReviewsController(ApplicationDbContext context) => _context = context;

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string? title, string? comment)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Category != null && p.Category.IsActive);
            if (product == null) return NotFound();

            title = title?.Trim();
            comment = comment?.Trim();
            if (rating is < 1 or > 5 || string.IsNullOrWhiteSpace(comment) || comment.Length > 2000 || (title?.Length ?? 0) > 120)
            {
                TempData["ErrorMessage"] = "Enter a rating from 1 to 5 and a comment of up to 2,000 characters.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            _context.ProductReviews.Add(new ProductReview
            {
                ProductId = productId,
                ReviewerName = User.Identity?.Name ?? "Customer",
                Rating = rating,
                Title = string.IsNullOrWhiteSpace(title) ? null : title,
                Comment = comment,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thanks for your review. It will appear after it is approved.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}
