using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        public string? ThumbnailUrl { get; set; }
        public string? ThumbnailPublicId { get; set; }
        public int Stock { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public DateTime CreatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        public DateTime UpdatedAt { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
    }
}