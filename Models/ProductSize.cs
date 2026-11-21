using System.ComponentModel.DataAnnotations;
namespace SuperShop.Models
{
    public class ProductSize
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        [Required, StringLength(30)] public string Name { get; set; } = string.Empty;
    }
}
