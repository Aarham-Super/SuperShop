using System.ComponentModel.DataAnnotations;
namespace SuperShop.Models
{
    public class PasswordResetCode
    {
        public int Id { get; set; }
        [Required, StringLength(255)] public string UserId { get; set; } = string.Empty;
        [Required, StringLength(64)] public string CodeHash { get; set; } = string.Empty;
        [Required, StringLength(64)] public string Salt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public int FailedAttempts { get; set; }
    }
}
