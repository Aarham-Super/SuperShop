using SuperShop.Models;
namespace SuperShop.ViewModels
{
    public class CartPageViewModel
    {
        public List<CartLineViewModel> Items { get; set; } = new();
        public string? PromotionCode { get; set; }
        public int PromotionPercent { get; set; }
        public decimal Subtotal => Items.Sum(item => item.LineTotal);
        public decimal DiscountAmount => Math.Round(Subtotal * PromotionPercent / 100m, 2, MidpointRounding.AwayFromZero);
        public decimal Total => Subtotal - DiscountAmount;
    }
    public class CartLineViewModel
    {
        public Product Product { get; set; } = null!;
        public ProductImage? Image { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public decimal UnitPrice => Product.SalePrice ?? Product.Price;
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
