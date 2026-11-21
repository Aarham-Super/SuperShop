namespace SuperShop.ViewModels
{
    public class AdminCustomerRowViewModel
    {
        public SuperShop.Models.ApplicationUser User { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal DemoSpend { get; set; }
    }
}
