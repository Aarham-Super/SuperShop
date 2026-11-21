using System.ComponentModel.DataAnnotations;

namespace SuperShop.Models
{
    public class ProductColor
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        [Required, StringLength(50)] public string Name { get; set; } = string.Empty;
    }
}
