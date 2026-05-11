using System.Collections.Generic;

namespace SmartShop.Models.ViewModels
{
    public class ProductDetailVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public bool IsFeatured { get; set; }
        public int SoldCount { get; set; }
        public List<ProductAttributeGroupVM> AttributeGroups { get; set; } = new();
        public List<ProductItemVM> RelatedProducts { get; set; } = new();
        public decimal DisplayPrice => SalePrice ?? BasePrice;
        public bool IsOnSale => SalePrice.HasValue && SalePrice < BasePrice;
        public int DiscountPercent => IsOnSale
            ? (int)System.Math.Round((1 - SalePrice!.Value / BasePrice) * 100)
            : 0;
    }
    public class ProductAttributeGroupVM
    {
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDisplayName { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
    }
}
