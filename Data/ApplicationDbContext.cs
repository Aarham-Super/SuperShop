using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SuperShop.Models;

namespace SuperShop.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // PRODUCT CATALOGUE
        // =========================================================

        public DbSet<Category> Categories
        {
            get;
            set;
        }

        public DbSet<Product> Products
        {
            get;
            set;
        }

        public DbSet<ProductImage> ProductImages
        {
            get;
            set;
        }

        public DbSet<Inventory> Inventory
        {
            get;
            set;
        }

        public DbSet<SiteBanner> SiteBanners
        {
            get;
            set;
        }

        public DbSet<DemoOrder> DemoOrders { get; set; }

        public DbSet<ProductReview> ProductReviews { get; set; }

        public DbSet<ProductColor> ProductColors { get; set; }

        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        public DbSet<WishlistItem> WishlistItems { get; set; }

        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<ProductSize> ProductSizes { get; set; }

        public DbSet<DemoOrderItem> DemoOrderItems { get; set; }

        // =========================================================
        // DATABASE RELATIONSHIPS
        // =========================================================

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // -----------------------------------------------------
            // CATEGORY
            // -----------------------------------------------------

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // -----------------------------------------------------
            // PRODUCT SKU
            // -----------------------------------------------------

            builder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            // -----------------------------------------------------
            // PRODUCT → CATEGORY
            // -----------------------------------------------------

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // -----------------------------------------------------
            // PRODUCT → INVENTORY
            // One product has one inventory record
            // -----------------------------------------------------

            builder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithOne(p => p.Inventory)
                .HasForeignKey<Inventory>(
                    i => i.ProductId
                )
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------------------------------------
            // PRODUCT → IMAGES
            // One product can have multiple images
            // -----------------------------------------------------

            builder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // -----------------------------------------------------
            builder.Entity<PasswordResetCode>()
                .HasOne<ApplicationUser>().WithMany()
                .HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PasswordResetCode>().HasIndex(code => new { code.UserId, code.CreatedAt });
            builder.Entity<WishlistItem>()
                .HasOne(item => item.User).WithMany(user => user.WishlistItems)
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WishlistItem>()
                .HasOne(item => item.Product).WithMany(product => product.WishlistItems)
                .HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<WishlistItem>().HasIndex(item => new { item.UserId, item.ProductId }).IsUnique();
            builder.Entity<Promotion>().HasIndex(promotion => promotion.Code).IsUnique();

            builder.Entity<DemoOrderItem>()
                .HasOne(item => item.DemoOrder)
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.DemoOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductSize>()
                .HasOne(size => size.Product)
                .WithMany(product => product.Sizes)
                .HasForeignKey(size => size.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductColor>()
                .HasOne(color => color.Product)
                .WithMany(product => product.Colors)
                .HasForeignKey(color => color.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasOne(review => review.Product)
                .WithMany(product => product.Reviews)
                .HasForeignKey(review => review.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DemoOrder>().HasIndex(order => order.UserId);

            builder.Entity<ProductReview>().HasIndex(review => new { review.ProductId, review.IsApproved });

            // MONEY PRECISION
            // -----------------------------------------------------

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasPrecision(18, 2);
        }
    }
}


