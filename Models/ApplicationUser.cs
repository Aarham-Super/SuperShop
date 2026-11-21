using Microsoft.AspNetCore.Identity;

namespace SuperShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public string? FullName { get; set; }
    }
}