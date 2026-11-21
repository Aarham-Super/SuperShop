using System.ComponentModel.DataAnnotations;

namespace SuperShop.ViewModels
{
    public class AccountSettingsViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;


        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;


        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
    }
}