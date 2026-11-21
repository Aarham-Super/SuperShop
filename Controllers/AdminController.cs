using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SuperShop.Data;
using SuperShop.Models;
using SuperShop.ViewModels;

namespace SuperShop.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<AdminController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // ============================================================
        // ADMIN DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var productCount = await _context.Products.CountAsync();
            var categoryCount = await _context.Categories.CountAsync();
            var customerCount = await _context.Users.CountAsync();
            var orderCount = await _context.DemoOrders.CountAsync();
            var itemsSold = await _context.DemoOrders.SumAsync(order => (int?)order.ItemsSold) ?? 0;
            var demoRevenue = await _context.DemoOrders.SumAsync(order => (decimal?)order.TotalAmount) ?? 0m;

            ViewBag.ProductCount = productCount;
            ViewBag.CategoryCount = categoryCount;
            ViewBag.CustomerCount = customerCount;
            ViewBag.OrderCount = orderCount;
            ViewBag.ItemsSold = itemsSold;
            ViewBag.DemoRevenue = demoRevenue;
            return View();
        }


        // ============================================================
        // PRODUCT MANAGEMENT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Colors)
                .Include(p => p.Sizes)
                .Include(p => p.Inventory)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }


        // ============================================================
        // ADD PRODUCT - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            await LoadCategoriesAsync();

            return View(new AdminProductCreateViewModel());
        }


        // ============================================================
        // ADD PRODUCT - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            AdminProductCreateViewModel model)
        {
            try
            {
                // ----------------------------------------------------
                // Validate price
                // ----------------------------------------------------

                if (model.Price < 0)
                {
                    ModelState.AddModelError(
                        nameof(model.Price),
                        "Price cannot be negative.");
                }

                // ----------------------------------------------------
                // Validate sale price
                // ----------------------------------------------------

                if (model.SalePrice.HasValue &&
                    model.SalePrice.Value < 0)
                {
                    ModelState.AddModelError(
                        nameof(model.SalePrice),
                        "Sale price cannot be negative.");
                }

                if (model.SalePrice.HasValue &&
                    model.SalePrice.Value > model.Price)
                {
                    ModelState.AddModelError(
                        nameof(model.SalePrice),
                        "Sale price cannot be greater than the regular price.");
                }

                // ----------------------------------------------------
                // Validate stock
                // ----------------------------------------------------

                if (model.StockQuantity < 0)
                {
                    ModelState.AddModelError(
                        nameof(model.StockQuantity),
                        "Stock quantity cannot be negative.");
                }

                // ----------------------------------------------------
                // Validate category
                // ----------------------------------------------------

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.Id == model.CategoryId &&
                        c.IsActive);

                if (category == null)
                {
                    ModelState.AddModelError(
                        nameof(model.CategoryId),
                        "Please select a valid active category.");
                }

                // ----------------------------------------------------
                // Validate SKU
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(model.SKU))
                {
                    var sku = model.SKU.Trim();

                    var skuExists = await _context.Products
                        .AnyAsync(p =>
                            p.SKU != null &&
                            p.SKU == sku);

                    if (skuExists)
                    {
                        ModelState.AddModelError(
                            nameof(model.SKU),
                            "This SKU is already in use.");
                    }
                }

                // ----------------------------------------------------
                // Return form if validation failed
                // ----------------------------------------------------

                NormalizeAndValidateColors(model);

                if (!ModelState.IsValid)
                {
                    await LoadCategoriesAsync();

                    return View(model);
                }

                // ----------------------------------------------------
                // Create product
                // ----------------------------------------------------

                var product = new Product
                {
                    Name = model.Name.Trim(),

                    ShortDescription =
                        string.IsNullOrWhiteSpace(model.ShortDescription)
                            ? null
                            : model.ShortDescription.Trim(),

                    Description =
                        string.IsNullOrWhiteSpace(model.Description)
                            ? null
                            : model.Description.Trim(),

                    Price = model.Price,

                    SalePrice = model.SalePrice,

                    CategoryId = model.CategoryId,


                    SKU =
                        string.IsNullOrWhiteSpace(model.SKU)
                            ? null
                            : model.SKU.Trim(),

                    IsActive = model.IsActive,

                    IsFeatured = model.IsFeatured,

                    Colors = model.Colors.Take(20).Select(color => new ProductColor { Name = color }).ToList(),
                    Sizes = model.Sizes.Take(20).Select(size => new ProductSize { Name = size }).ToList(),

                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);

                await _context.SaveChangesAsync();


                // ----------------------------------------------------
                // Create inventory record
                // ----------------------------------------------------

                var inventory = new Inventory
                {
                    ProductId = product.Id,

                    StockQuantity = model.StockQuantity,

                    LowStockThreshold = model.LowStockThreshold,

                    UpdatedAt = DateTime.UtcNow
                };

                _context.Inventory.Add(inventory);

                await _context.SaveChangesAsync();


                // ----------------------------------------------------
                // Upload product images
                // ----------------------------------------------------

                if (model.Images != null &&
                    model.Images.Count > 0)
                {
                    var uploadsFolder = Path.Combine(
                        _environment.WebRootPath,
                        "uploads",
                        "products");

                    Directory.CreateDirectory(uploadsFolder);

                    var displayOrder = 0;

                    foreach (var image in model.Images)
                    {
                        if (image == null ||
                            image.Length == 0)
                        {
                            continue;
                        }

                        var extension =
                            Path.GetExtension(image.FileName)
                                .ToLowerInvariant();

                        var allowedExtensions = new[]
                        {
                            ".jpg",
                            ".jpeg",
                            ".png",
                            ".webp"
                        };

                        if (!allowedExtensions.Contains(extension))
                        {
                            continue;
                        }

                        // Maximum 5 MB per image.
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            continue;
                        }

                        var fileName =
                            $"{Guid.NewGuid():N}{extension}";

                        var filePath =
                            Path.Combine(
                                uploadsFolder,
                                fileName);

                        await using (
                            var stream =
                                new FileStream(
                                    filePath,
                                    FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,

                            ImageUrl =
                                $"/uploads/products/{fileName}",

                            AltText = product.Name,

                            IsPrimary =
                                displayOrder == 0,

                            DisplayOrder = displayOrder
                        };

                        _context.ProductImages.Add(productImage);

                        displayOrder++;
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] =
                    $"Product '{product.Name}' was added successfully.";

                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while adding a product.");

                ModelState.AddModelError(
                    string.Empty,
                    "The product could not be saved. Please try again.");

                await LoadCategoriesAsync();

                return View(model);
            }
        }


        // ============================================================
        // EDIT PRODUCT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Inventory)
                .Include(p => p.Images)
                .Include(p => p.Colors)
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            await LoadCategoriesAsync();
            ViewBag.IsEdit = true;
            ViewBag.ProductId = product.Id;
            ViewBag.ExistingImages = product.Images.ToList();

            return View("AddProduct", new AdminProductCreateViewModel
            {
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                Price = product.Price,
                SalePrice = product.SalePrice,
                CategoryId = product.CategoryId,
                Colors = product.Colors.Select(color => color.Name).ToList(),
                Sizes = product.Sizes.Select(size => size.Name).ToList(),
                SKU = product.SKU,
                StockQuantity = product.Inventory?.StockQuantity ?? 0,
                LowStockThreshold = product.Inventory?.LowStockThreshold ?? 5,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, AdminProductCreateViewModel model)
        {
            var product = await _context.Products
                .Include(p => p.Inventory)
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.Colors)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            var hasNewImages = model.Images?.Any(image => image != null && image.Length > 0) == true;
            if (!hasNewImages)
            {
                model.Images = new List<IFormFile>();
                foreach (var key in ModelState.Keys
                             .Where(key => key.Equals(nameof(model.Images), StringComparison.OrdinalIgnoreCase) ||
                                           key.StartsWith(nameof(model.Images) + "[", StringComparison.OrdinalIgnoreCase))
                             .ToList())
                {
                    ModelState.Remove(key);
                }
            }

            NormalizeAndValidateColors(model);

            var categoryIsActive = await _context.Categories
                .AnyAsync(c => c.Id == model.CategoryId && c.IsActive);
            if (!categoryIsActive)
                ModelState.AddModelError(nameof(model.CategoryId), "Please select a valid active category.");

            if (model.SalePrice.HasValue && model.SalePrice.Value > model.Price)
                ModelState.AddModelError(nameof(model.SalePrice), "Sale price cannot be greater than the regular price.");

            if (!string.IsNullOrWhiteSpace(model.SKU))
            {
                var normalizedSku = model.SKU.Trim();
                if (await _context.Products.AnyAsync(p => p.Id != id && p.SKU != null && p.SKU == normalizedSku))
                    ModelState.AddModelError(nameof(model.SKU), "This SKU is already in use.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                ViewBag.IsEdit = true;
                ViewBag.ProductId = id;
                ViewBag.ExistingImages = product.Images.ToList();
                return View("AddProduct", model);
            }

            try
            {
                var removedIds = model.RemovedImageIds.Distinct().ToHashSet();
                var removedImages = product.Images.Where(image => removedIds.Contains(image.Id)).ToList();
                foreach (var image in removedImages)
                {
                    _context.ProductImages.Remove(image);
                    product.Images.Remove(image);
                }
                if (!product.Images.Any(image => image.IsPrimary) && product.Images.FirstOrDefault() is { } firstRemainingImage)
                    firstRemainingImage.IsPrimary = true;

                product.Name = model.Name.Trim();
                product.ShortDescription = string.IsNullOrWhiteSpace(model.ShortDescription) ? null : model.ShortDescription.Trim();
                product.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
                product.Price = model.Price;
                product.SalePrice = model.SalePrice;
                product.CategoryId = model.CategoryId;
                product.SKU = string.IsNullOrWhiteSpace(model.SKU) ? null : model.SKU.Trim();
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;
                product.UpdatedAt = DateTime.UtcNow;

                _context.ProductColors.RemoveRange(product.Colors);
                product.Colors.Clear();
                foreach (var colorName in model.Colors)
                    product.Colors.Add(new ProductColor { Name = colorName });
                _context.ProductSizes.RemoveRange(product.Sizes);
                product.Sizes.Clear();
                foreach (var sizeName in model.Sizes)
                    product.Sizes.Add(new ProductSize { Name = sizeName });

                if (product.Inventory == null)
                {
                    product.Inventory = new Inventory { ProductId = product.Id };
                    _context.Inventory.Add(product.Inventory);
                }
                product.Inventory.StockQuantity = model.StockQuantity;
                product.Inventory.LowStockThreshold = model.LowStockThreshold;
                product.Inventory.UpdatedAt = DateTime.UtcNow;

                await SaveProductImagesAsync(product, model.Images ?? new List<IFormFile>());
                await _context.SaveChangesAsync();
                foreach (var image in removedImages)
                    DeleteProductImageFile(image);

                TempData["SuccessMessage"] = $"Product '{product.Name}' was updated successfully.";
                return RedirectToAction(nameof(Products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while editing product {ProductId}.", id);
                ModelState.AddModelError(string.Empty, "The product could not be saved. Please try again.");
                await LoadCategoriesAsync();
                ViewBag.IsEdit = true;
                ViewBag.ProductId = id;
                ViewBag.ExistingImages = product.Images.ToList();
                return View("AddProduct", model);
            }
        }

        private void DeleteProductImageFile(ProductImage image)
        {
            if (string.IsNullOrWhiteSpace(image.ImageUrl) || !image.ImageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
                return;

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "products", Path.GetFileName(image.ImageUrl));
            try
            {
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not delete product image file {ImagePath}.", filePath);
            }
        }
        private async Task SaveProductImagesAsync(Product product, IEnumerable<IFormFile> images)
        {
            var uploads = images?.Where(image => image != null && image.Length > 0).ToList();
            if (uploads == null || uploads.Count == 0) return;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            var displayOrder = product.Images.Count == 0 ? 0 : product.Images.Max(image => image.DisplayOrder) + 1;
            foreach (var image in uploads)
            {
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension) || image.Length > 5 * 1024 * 1024) continue;

                var fileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await image.CopyToAsync(stream);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = $"/uploads/products/{fileName}",
                    AltText = product.Name,
                    IsPrimary = product.Images.Count == 0 && displayOrder == 0,
                    DisplayOrder = displayOrder++
                });
            }
        }
        // ============================================================
        // CATEGORY MANAGEMENT
        // ============================================================

        // ------------------------------------------------------------
        // Categories - List
        // ------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }


        // ------------------------------------------------------------
        // Add Category
        // ------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(
            string name,
            string? description,
            int displayOrder = 0,
            bool isActive = true)
        {
            try
            {
                // ----------------------------------------------------
                // Validate name
                // ----------------------------------------------------

                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["ErrorMessage"] =
                        "Category name is required.";

                    return RedirectToAction(nameof(Categories));
                }

                name = name.Trim();


                // ----------------------------------------------------
                // Validate length
                // ----------------------------------------------------

                if (name.Length > 100)
                {
                    TempData["ErrorMessage"] =
                        "Category name cannot exceed 100 characters.";

                    return RedirectToAction(nameof(Categories));
                }


                // ----------------------------------------------------
                // Check duplicate category
                // ----------------------------------------------------

                var exists = await _context.Categories
                    .AnyAsync(c =>
                        c.Name.ToLower() == name.ToLower());

                if (exists)
                {
                    TempData["ErrorMessage"] =
                        $"A category named '{name}' already exists.";

                    return RedirectToAction(nameof(Categories));
                }


                // ----------------------------------------------------
                // Validate display order
                // ----------------------------------------------------

                if (displayOrder < 0)
                {
                    displayOrder = 0;
                }


                // ----------------------------------------------------
                // Validate description
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(description))
                {
                    description = description.Trim();

                    if (description.Length > 500)
                    {
                        TempData["ErrorMessage"] =
                            "Category description cannot exceed 500 characters.";

                        return RedirectToAction(nameof(Categories));
                    }
                }


                // ----------------------------------------------------
                // Create category
                // ----------------------------------------------------

                var category = new Category
                {
                    Name = name,

                    Description =
                        string.IsNullOrWhiteSpace(description)
                            ? null
                            : description,

                    DisplayOrder = displayOrder,

                    IsActive = isActive,

                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Category '{category.Name}' was created successfully.";

                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while adding category.");

                TempData["ErrorMessage"] =
                    "The category could not be created. Please try again.";

                return RedirectToAction(nameof(Categories));
            }
        }


        // ------------------------------------------------------------
        // Activate / Deactivate Category
        // ------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] =
                        "Category could not be found.";

                    return RedirectToAction(nameof(Categories));
                }

                category.IsActive = !category.IsActive;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    category.IsActive
                        ? $"Category '{category.Name}' is now active."
                        : $"Category '{category.Name}' has been deactivated.";

                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while changing category status for ID {CategoryId}.",
                    id);

                TempData["ErrorMessage"] =
                    "The category status could not be changed.";

                return RedirectToAction(nameof(Categories));
            }
        }


        // ------------------------------------------------------------
        // Delete Category
        // ------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] =
                        "Category could not be found.";

                    return RedirectToAction(nameof(Categories));
                }


                // ----------------------------------------------------
                // Never delete a category containing products.
                // This protects the product/category relationship.
                // ----------------------------------------------------

                if (category.Products.Any())
                {
                    TempData["ErrorMessage"] =
                        $"Category '{category.Name}' cannot be deleted because it contains products.";

                    return RedirectToAction(nameof(Categories));
                }


                var categoryName = category.Name;

                _context.Categories.Remove(category);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Category '{categoryName}' was deleted successfully.";

                return RedirectToAction(nameof(Categories));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while deleting category ID {CategoryId}.",
                    id);

                TempData["ErrorMessage"] =
                    "The category could not be deleted.";

                return RedirectToAction(nameof(Categories));
            }
        }


        // ============================================================
        // INVENTORY
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Inventory()
        {
            var inventory = await _context.Inventory
                .Include(i => i.Product)
                .ThenInclude(p => p!.Category)
                .OrderBy(i => i.StockQuantity)
                .ToListAsync();

            return View(inventory);
        }


        // ============================================================
        // ORDERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.DemoOrders.Include(order => order.Items).OrderByDescending(order => order.CreatedAt).ToListAsync();
            var userIds = orders.Where(order => !string.IsNullOrWhiteSpace(order.UserId)).Select(order => order.UserId!).Distinct().ToList();
            ViewBag.CustomerLabels = await _context.Users.Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, user => user.UserName ?? user.Email ?? "Customer");
            return View(orders);
        }


        // ============================================================
        // CUSTOMERS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var superAdminRoleIds = await _context.Roles.Where(role => role.Name == "SuperAdmin").Select(role => role.Id).ToListAsync();
            var superAdminUserIds = await _context.UserRoles.Where(userRole => superAdminRoleIds.Contains(userRole.RoleId)).Select(userRole => userRole.UserId).ToListAsync();
            var users = await _context.Users.Where(user => !superAdminUserIds.Contains(user.Id)).OrderByDescending(user => user.Id).ToListAsync();
            var stats = await _context.DemoOrders.Where(order => order.UserId != null).GroupBy(order => order.UserId!)
                .Select(group => new { UserId = group.Key, Count = group.Count(), Spend = group.Sum(order => order.TotalAmount) })
                .ToDictionaryAsync(group => group.UserId, group => new { group.Count, group.Spend });
            var rows = users.Select(user => new AdminCustomerRowViewModel
            {
                User = user,
                OrderCount = stats.TryGetValue(user.Id, out var stat) ? stat.Count : 0,
                DemoSpend = stats.TryGetValue(user.Id, out stat) ? stat.Spend : 0m
            }).ToList();
            return View(rows);
        }


        // ============================================================
        // REVIEWS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.ProductReviews
                .Include(review => review.Product)
                .OrderBy(review => review.IsApproved)
                .ThenByDescending(review => review.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null) { TempData["ErrorMessage"] = "Review not found."; return RedirectToAction(nameof(Reviews)); }
            review.IsApproved = !review.IsApproved;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = review.IsApproved ? "Review approved." : "Review hidden.";
            return RedirectToAction(nameof(Reviews));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review == null) { TempData["ErrorMessage"] = "Review not found."; return RedirectToAction(nameof(Reviews)); }
            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Review deleted.";
            return RedirectToAction(nameof(Reviews));
        }


        // ============================================================
        // PROMOTIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            return View(await _context.Promotions.OrderByDescending(promotion => promotion.IsActive).ThenByDescending(promotion => promotion.StartsAt).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPromotion(PromotionCreateViewModel model)
        {
            model.Code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Code, "^[A-Z0-9-]{3,32}$"))
                ModelState.AddModelError(nameof(model.Code), "Use 3–32 letters, numbers, or hyphens.");
            if (model.EndsAt.HasValue && model.EndsAt.Value <= model.StartsAt)
                ModelState.AddModelError(nameof(model.EndsAt), "The end date must be after the start date.");
            if (await _context.Promotions.AnyAsync(promotion => promotion.Code == model.Code))
                ModelState.AddModelError(nameof(model.Code), "That promotion code already exists.");
            if (!ModelState.IsValid)
            {
                ViewBag.NewPromotion = model;
                return View("Promotions", await _context.Promotions.OrderByDescending(promotion => promotion.StartsAt).ToListAsync());
            }
            _context.Promotions.Add(new Promotion { Code = model.Code, Description = model.Description?.Trim(), DiscountPercent = model.DiscountPercent, StartsAt = DateTime.SpecifyKind(model.StartsAt, DateTimeKind.Utc), EndsAt = model.EndsAt.HasValue ? DateTime.SpecifyKind(model.EndsAt.Value, DateTimeKind.Utc) : null, MaxUses = model.MaxUses, IsActive = true });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Promotion created.";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) { TempData["ErrorMessage"] = "Promotion not found."; return RedirectToAction(nameof(Promotions)); }
            promotion.IsActive = !promotion.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = promotion.IsActive ? "Promotion activated." : "Promotion deactivated.";
            return RedirectToAction(nameof(Promotions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null) { _context.Promotions.Remove(promotion); await _context.SaveChangesAsync(); TempData["SuccessMessage"] = "Promotion deleted."; }
            return RedirectToAction(nameof(Promotions));
        }


        // ============================================================
        // SETTINGS
        // ============================================================

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }


        // ============================================================
        // SITE BANNER
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Banner()
        {
            var banner = await _context.SiteBanners
                .OrderByDescending(b => b.UpdatedAt)
                .FirstOrDefaultAsync();

            return View(banner ?? new SiteBanner());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Banner(SiteBanner model)
        {
            model.Title = model.Title?.Trim() ?? string.Empty;
            model.Message = model.Message?.Trim() ?? string.Empty;
            model.ButtonText = string.IsNullOrWhiteSpace(model.ButtonText)
                ? null
                : model.ButtonText.Trim();
            model.ButtonUrl = string.IsNullOrWhiteSpace(model.ButtonUrl)
                ? null
                : model.ButtonUrl.Trim();

            if (model.ButtonUrl != null && !Url.IsLocalUrl(model.ButtonUrl))
            {
                ModelState.AddModelError(
                    nameof(model.ButtonUrl),
                    "Use a link within this website, such as /Products.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var banner = await _context.SiteBanners
                .OrderByDescending(b => b.UpdatedAt)
                .FirstOrDefaultAsync();

            if (banner == null)
            {
                banner = new SiteBanner();
                _context.SiteBanners.Add(banner);
            }

            banner.Title = model.Title;
            banner.Message = model.Message;
            banner.ButtonText = model.ButtonText;
            banner.ButtonUrl = model.ButtonUrl;
            banner.IsActive = model.IsActive;
            banner.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Website banner settings were saved.";
            return RedirectToAction(nameof(Banner));
        }


        // ============================================================
        // PRIVATE HELPERS
        // ============================================================

        private void NormalizeAndValidateColors(AdminProductCreateViewModel model)
        {
            model.Colors ??= new List<string>();
            model.Colors = model.Colors.Where(color => !string.IsNullOrWhiteSpace(color)).Select(color => color.Trim()).ToList();
            if (model.Colors.Count > 20 || model.Colors.Any(color => color.Length > 50))
                ModelState.AddModelError(nameof(model.Colors), "Add up to 20 colors, with each color 50 characters or fewer.");
            model.Colors = model.Colors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            model.Sizes ??= new List<string>();
            model.Sizes = model.Sizes.Where(size => !string.IsNullOrWhiteSpace(size)).Select(size => size.Trim()).ToList();
            if (model.Sizes.Count > 20 || model.Sizes.Any(size => size.Length > 30))
                ModelState.AddModelError(nameof(model.Sizes), "Add up to 20 sizes, with each size 30 characters or fewer.");
            model.Sizes = model.Sizes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = new SelectList(
                categories,
                "Id",
                "Name");
        }
    }
}






