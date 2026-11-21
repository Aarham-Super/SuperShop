using System.ComponentModel.DataAnnotations.Schema;
namespace SuperShop.Models
{
    public class DemoOrderItem
    {
        public int Id { get; set; }
        public int DemoOrderId { get; set; }
        public DemoOrder? DemoOrder { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal LineTotal { get; set; }
    }
}
