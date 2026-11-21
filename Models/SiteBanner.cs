using System.ComponentModel.DataAnnotations;

namespace SuperShop.Models
{
    public class SiteBanner
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ButtonText { get; set; }

        [StringLength(400)]
        public string? ButtonUrl { get; set; }

        public bool IsActive { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
