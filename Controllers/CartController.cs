using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using SuperShop.Models;
using SuperShop.ViewModels;

namespace SuperShop.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "SuperShop.Cart.v2";
        private const string PromoSessionKey = "SuperShop.Promo";
        private readonly ApplicationDbContext _context;
        public CartController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index() => View(await BuildCartModelAsync());

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var model = await BuildCartModelAsync();
            if (!model.Items.Any())
            {
                TempData["ErrorMessage"] = "Add an item to your cart before checkout.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1, string? size = null)
        {
            var product = await _context.Products.Include(p => p.Inventory).Include(p => p.Category).Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Category != null && p.Category.IsActive);
            if (product == null) { TempData["ErrorMessage"] = "That product is no longer available."; return RedirectToAction("Index", "Products"); }
            if (product.Inventory == null || product.Inventory.StockQuantity < 1) { TempData["ErrorMessage"] = "This product is currently out of stock."; return RedirectToAction("Details", "Products", new { id = productId }); }

            if (product.Sizes.Count > 0)
            {
                var validSize = product.Sizes.FirstOrDefault(s => s.Name.Equals(size?.Trim(), StringComparison.OrdinalIgnoreCase));
                if (validSize == null) { TempData["ErrorMessage"] = "Choose a size before adding this item to your cart."; return RedirectToAction("Details", "Products", new { id = productId }); }
                size = validSize.Name;
            }
            else size = null;

            quantity = Math.Clamp(quantity, 1, product.Inventory!.StockQuantity);
            var cart = ReadCart();
            var line = cart.FirstOrDefault(item => item.ProductId == productId && string.Equals(item.Size, size, StringComparison.OrdinalIgnoreCase));
            var totalForProduct = cart.Where(item => item.ProductId == productId).Sum(item => item.Quantity);
            if (totalForProduct + quantity > product.Inventory.StockQuantity)
            {
                TempData["ErrorMessage"] = $"Only {product.Inventory.StockQuantity} of this item are currently available.";
                return RedirectToAction(nameof(Index));
            }
            if (line == null) cart.Add(new CartEntry { ProductId = productId, Size = size, Quantity = quantity });
            else line.Quantity += quantity;
            WriteCart(cart);
            TempData["SuccessMessage"] = $"{product.Name} added to your cart.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutSubmit()
        {
            var cart = ReadCart();
            if (cart.Count == 0) { TempData["ErrorMessage"] = "Your cart is empty."; return RedirectToAction(nameof(Index)); }
            if (cart.Any(item => item.Quantity < 1)) { TempData["ErrorMessage"] = "The cart contains an invalid quantity."; return RedirectToAction(nameof(Index)); }

            var productIds = cart.Select(item => item.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id) && p.IsActive && p.Category != null && p.Category.IsActive)
                .Include(p => p.Inventory).Include(p => p.Sizes).ToListAsync();
            if (products.Count != productIds.Count) { TempData["ErrorMessage"] = "A cart item is no longer available. Please review your cart."; return RedirectToAction(nameof(Index)); }
            foreach (var product in products)
            {
                var wanted = cart.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
                if (product.Inventory == null || product.Inventory.StockQuantity < wanted || cart.Where(item => item.ProductId == product.Id).Any(item => product.Sizes.Count > 0 && !product.Sizes.Any(size => size.Name.Equals(item.Size, StringComparison.OrdinalIgnoreCase))))
                { TempData["ErrorMessage"] = $"{product.Name} is no longer available in the selected quantity or size."; return RedirectToAction(nameof(Index)); }
            }

            var promoCode = HttpContext.Session.GetString(PromoSessionKey);
            var now = DateTime.UtcNow;
            var promotion = string.IsNullOrWhiteSpace(promoCode) ? null : await _context.Promotions.FirstOrDefaultAsync(p => p.Code == promoCode && p.IsActive && p.StartsAt <= now && (p.EndsAt == null || p.EndsAt > now) && (p.MaxUses == null || p.Uses < p.MaxUses));
            if (!string.IsNullOrWhiteSpace(promoCode) && promotion == null)
            {
                HttpContext.Session.Remove(PromoSessionKey);
                TempData["ErrorMessage"] = "That promotion is no longer valid. Review your cart and try again.";
                return RedirectToAction(nameof(Index));
            }

            var itemCount = cart.Sum(item => item.Quantity);
            var order = new DemoOrder { CreatedAt = now, ItemsSold = itemCount, UserId = User.FindFirstValue(ClaimTypes.NameIdentifier), PromotionCode = promotion?.Code };
            foreach (var entry in cart)
            {
                var product = products.Single(p => p.Id == entry.ProductId);
                var price = product.SalePrice ?? product.Price;
                order.Items.Add(new DemoOrderItem { ProductName = product.Name, Size = entry.Size, Quantity = entry.Quantity, UnitPrice = price, LineTotal = price * entry.Quantity });
            }
            var subtotal = order.Items.Sum(item => item.LineTotal);
            order.DiscountAmount = promotion == null ? 0m : Math.Round(subtotal * promotion.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
            order.TotalAmount = subtotal - order.DiscountAmount;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            foreach (var product in products)
            {
                product.Inventory!.StockQuantity -= cart.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
                product.Inventory.UpdatedAt = DateTime.UtcNow;
            }
            if (promotion != null)
            {
                var claimed = await _context.Promotions.Where(p => p.Id == promotion.Id && p.IsActive && p.StartsAt <= now && (p.EndsAt == null || p.EndsAt > now) && (p.MaxUses == null || p.Uses < p.MaxUses))
                    .ExecuteUpdateAsync(update => update.SetProperty(p => p.Uses, p => p.Uses + 1));
                if (claimed != 1)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "That promotion has reached its use limit. Remove it and try again.";
                    return RedirectToAction(nameof(Index));
                }
            }
            _context.DemoOrders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(PromoSessionKey);
            HttpContext.Session.Remove("SuperShop.Cart");
            TempData["SuccessMessage"] = $"Demo purchase recorded: {itemCount} item(s). No payment was processed and card details were not sent or saved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPromotion(string code)
        {
            var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            var now = DateTime.UtcNow;
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == normalized && p.IsActive && p.StartsAt <= now && (p.EndsAt == null || p.EndsAt > now) && (p.MaxUses == null || p.Uses < p.MaxUses));
            if (promotion == null)
            {
                TempData["ErrorMessage"] = "That promotion code is invalid or unavailable.";
                return RedirectToAction(nameof(Index));
            }
            HttpContext.Session.SetString(PromoSessionKey, promotion.Code);
            TempData["SuccessMessage"] = $"Promotion applied: {promotion.DiscountPercent}% off.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePromotion()
        {
            HttpContext.Session.Remove(PromoSessionKey);
            TempData["SuccessMessage"] = "Promotion removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, string? size, int quantity)
        {
            var cart = ReadCart();
            var line = cart.FirstOrDefault(item => item.ProductId == productId && string.Equals(item.Size, size, StringComparison.OrdinalIgnoreCase));
            if (line == null) return RedirectToAction(nameof(Index));
            if (quantity < 1) cart.Remove(line);
            else
            {
                var stock = await _context.Products.Where(p => p.Id == productId).Select(p => p.Inventory == null ? 0 : p.Inventory.StockQuantity).FirstOrDefaultAsync();
                var otherLines = cart.Where(item => item.ProductId == productId && item != line).Sum(item => item.Quantity);
                line.Quantity = Math.Clamp(quantity, 1, Math.Max(1, stock - otherLines));
            }
            WriteCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, string? size)
        {
            var cart = ReadCart();
            var line = cart.FirstOrDefault(item => item.ProductId == productId && string.Equals(item.Size, size, StringComparison.OrdinalIgnoreCase));
            if (line != null) cart.Remove(line);
            WriteCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear() { HttpContext.Session.Remove(CartSessionKey); HttpContext.Session.Remove(PromoSessionKey); HttpContext.Session.Remove("SuperShop.Cart"); return RedirectToAction(nameof(Index)); }

        private async Task<CartPageViewModel> BuildCartModelAsync()
        {
            var cart = ReadCart();
            var ids = cart.Select(item => item.ProductId).Distinct().ToList();
            var products = await _context.Products.Where(product => ids.Contains(product.Id) && product.IsActive && product.Category != null && product.Category.IsActive)
                .Include(product => product.Category).Include(product => product.Inventory).Include(product => product.Images).Include(product => product.Sizes).ToListAsync();
            var lines = new List<CartLineViewModel>();
            foreach (var entry in cart)
            {
                var product = products.FirstOrDefault(item => item.Id == entry.ProductId);
                if (product == null || product.Inventory == null || product.Inventory.StockQuantity < 1) continue;
                var image = product.Images.OrderByDescending(item => item.IsPrimary).ThenBy(item => item.DisplayOrder).FirstOrDefault();
                lines.Add(new CartLineViewModel { Product = product, Quantity = Math.Min(entry.Quantity, product.Inventory!.StockQuantity), Image = image, Size = entry.Size });
            }
            var viewModel = new CartPageViewModel { Items = lines };
            var savedCode = HttpContext.Session.GetString(PromoSessionKey);
            if (!string.IsNullOrWhiteSpace(savedCode))
            {
                var now = DateTime.UtcNow;
                var promotion = await _context.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == savedCode && p.IsActive && p.StartsAt <= now && (p.EndsAt == null || p.EndsAt > now) && (p.MaxUses == null || p.Uses < p.MaxUses));
                if (promotion == null) HttpContext.Session.Remove(PromoSessionKey);
                else { viewModel.PromotionCode = promotion.Code; viewModel.PromotionPercent = promotion.DiscountPercent; }
            }
            return viewModel;
        }

        private List<CartEntry> ReadCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try { return JsonSerializer.Deserialize<List<CartEntry>>(json) ?? new List<CartEntry>(); }
                catch (JsonException) { }
            }
            // Migrate a cart created by the earlier product-only session format.
            var legacy = HttpContext.Session.GetString("SuperShop.Cart");
            if (!string.IsNullOrWhiteSpace(legacy))
            {
                try { var migrated = (JsonSerializer.Deserialize<Dictionary<int, int>>(legacy) ?? new()).Select(pair => new CartEntry { ProductId = pair.Key, Quantity = pair.Value }).ToList(); HttpContext.Session.Remove("SuperShop.Cart"); return migrated; }
                catch (JsonException) { }
            }
            return new List<CartEntry>();
        }
        private void WriteCart(List<CartEntry> cart) => HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        public sealed class CartEntry { public int ProductId { get; set; } public string? Size { get; set; } public int Quantity { get; set; } }
    }
}
