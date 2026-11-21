using System.ComponentModel.DataAnnotations;
namespace SuperShop.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    }
    public class ResetPasswordViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, RegularExpression("^[0-9]{6}$", ErrorMessage = "Enter the 6-digit code from your email.")] public string Code { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
        [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = string.Empty;
    }
    public class PromotionCreateViewModel
    {
        [Required, StringLength(32)] public string Code { get; set; } = string.Empty;
        [StringLength(120)] public string? Description { get; set; }
        [Range(1, 90)] public int DiscountPercent { get; set; } = 10;
        [Required] public DateTime StartsAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndsAt { get; set; }
        [Range(1, 1000000)] public int? MaxUses { get; set; }
    }
}
