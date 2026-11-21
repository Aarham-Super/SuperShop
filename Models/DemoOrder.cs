using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperShop.Models
{
    public class DemoOrder
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ItemsSold { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        [StringLength(450)] public string? UserId { get; set; }
        public ICollection<DemoOrderItem> Items { get; set; } = new List<DemoOrderItem>();
        [StringLength(32)] public string? PromotionCode { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal DiscountAmount { get; set; }
    }
}

