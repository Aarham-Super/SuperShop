using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999)]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999)]
        public decimal? SalePrice { get; set; }

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        [StringLength(100)]
        public string? SKU { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ProductImage> Images { get; set; }
            = new List<ProductImage>();

        public Inventory? Inventory { get; set; }

        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();

        public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    }
}