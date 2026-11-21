using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SuperShop.ViewModels
{
    public class AdminProductCreateViewModel
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(200)]
        [Display(Name = "Product name")]
        public string Name { get; set; } = string.Empty;


        [StringLength(1000)]
        [Display(Name = "Short description")]
        public string? ShortDescription { get; set; }


        [Display(Name = "Description")]
        public string? Description { get; set; }


        [Required(ErrorMessage = "Price is required.")]
        [Range(
            0,
            999999999,
            ErrorMessage = "Please enter a valid price."
        )]
        [Display(Name = "Price")]
        public decimal Price { get; set; }


        [Range(
            0,
            999999999,
            ErrorMessage = "Please enter a valid sale price."
        )]
        [Display(Name = "Sale price")]
        public decimal? SalePrice { get; set; }


        [Display(Name = "Product colors (optional)")]
        public List<string> Colors { get; set; } = new();

        [Display(Name = "Available sizes (optional)")]
        public List<string> Sizes { get; set; } = new();


        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }


        [StringLength(100)]
        [Display(Name = "SKU")]
        public string? SKU { get; set; }


        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "Stock quantity cannot be negative."
        )]
        [Display(Name = "Initial stock quantity")]
        public int StockQuantity { get; set; }


        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "Low-stock threshold cannot be negative."
        )]
        [Display(Name = "Low-stock threshold")]
        public int LowStockThreshold { get; set; } = 5;


        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;


        [Display(Name = "Featured product")]
        public bool IsFeatured { get; set; }


        [Display(Name = "Product images")]
        public List<IFormFile>? Images { get; set; } = new();

        public List<int> RemovedImageIds { get; set; } = new();
    }
}

