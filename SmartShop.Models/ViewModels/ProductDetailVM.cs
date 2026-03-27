using System.Collections.Generic;

namespace SmartShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the product detail page (Shop/Detail/{slug}).
    /// </summary>
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

        /// <summary>Attribute groups for display (e.g. Chất liệu: Titanium).</summary>
        public List<ProductAttributeGroupVM> AttributeGroups { get; set; } = new();

        /// <summary>Related products from the same primary category.</summary>
        public List<ProductItemVM> RelatedProducts { get; set; } = new();

        // Computed helpers
        public decimal DisplayPrice => SalePrice ?? BasePrice;
        public bool IsOnSale => SalePrice.HasValue && SalePrice < BasePrice;
        public int DiscountPercent => IsOnSale
            ? (int)System.Math.Round((1 - SalePrice!.Value / BasePrice) * 100)
            : 0;
    }

    /// <summary>
    /// A single attribute group shown on the detail page
    /// (e.g. AttributeDisplayName = "Chất liệu", Values = ["Titanium", "Nhựa"]).
    /// </summary>
    public class ProductAttributeGroupVM
    {
        public string AttributeName { get; set; } = string.Empty;
        public string AttributeDisplayName { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
    }
}
