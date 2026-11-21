using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SuperShop.Models
{
    public class Promotion
    {
        public int Id { get; set; }
        [Required, StringLength(32)] public string Code { get; set; } = string.Empty;
        [StringLength(120)] public string? Description { get; set; }
        [Range(1, 90)] public int DiscountPercent { get; set; }
        public DateTime StartsAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int? MaxUses { get; set; }
        public int Uses { get; set; }
    }
}
