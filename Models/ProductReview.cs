using System.ComponentModel.DataAnnotations;

namespace SuperShop.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        [Required, StringLength(100)] public string ReviewerName { get; set; } = string.Empty;
        [Range(1, 5)] public int Rating { get; set; }
        [StringLength(120)] public string? Title { get; set; }
        [Required, StringLength(2000)] public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
