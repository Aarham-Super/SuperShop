using System.ComponentModel.DataAnnotations;

namespace SuperShop.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int LowStockThreshold { get; set; } = 5;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}