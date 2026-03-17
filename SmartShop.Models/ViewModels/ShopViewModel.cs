using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Shop — danh sách sản phẩm + bộ lọc động
    /// </summary>
    public class ShopViewModel
    {
        // ===== Danh sách sản phẩm =====
        public List<ProductItemVM> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // ===== Bộ lọc thuộc tính (chỉ khi đã chọn danh mục) =====
        public List<AttributeFilterVM> Attributes { get; set; } = new();

        // ===== Khoảng giá =====
        public List<PriceRangeVM> PriceRanges { get; set; } = new();

        // ===== Breadcrumb danh mục đã chọn (root → leaf) =====
        public List<CategoryBreadcrumbItemVM> SelectedCategoryBreadcrumb { get; set; } = new();

        // ===== Giá trị bộ lọc đã chọn =====
        public ShopFilterVM Filter { get; set; } = new();

        // ===== Sắp xếp =====
        public string SortBy { get; set; } = "newest";
    }

    /// <summary>
    /// Một bước trong breadcrumb danh mục
    /// </summary>
    public class CategoryBreadcrumbItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin sản phẩm hiển thị trong danh sách
    /// </summary>
    public class ProductItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? SalePrice { get; set; }
        public int Stock { get; set; }
        public bool IsFeatured { get; set; }

        public decimal DisplayPrice => SalePrice ?? BasePrice;
        public bool IsOnSale => SalePrice.HasValue && SalePrice < BasePrice;
        public int DiscountPercent => IsOnSale
            ? (int)Math.Round((1 - (SalePrice!.Value / BasePrice)) * 100)
            : 0;
    }

    /// <summary>
    /// Thuộc tính (Attribute) cho bộ lọc — ví dụ: Kiểu gọng, Chất liệu, Giới tính
    /// </summary>
    public class AttributeFilterVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<AttributeValueFilterVM> Values { get; set; } = new();
    }

    /// <summary>
    /// Giá trị thuộc tính cho bộ lọc
    /// </summary>
    public class AttributeValueFilterVM
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;        // code, e.g. 'titanium'
        public string DisplayName { get; set; } = string.Empty;  // label, e.g. 'Titanium'
        public int ProductCount { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Khoảng giá cho bộ lọc
    /// </summary>
    public class PriceRangeVM
    {
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Label { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public bool IsSelected { get; set; }

        public string Value => MaxPrice.HasValue
            ? $"{MinPrice}-{MaxPrice}"
            : $"{MinPrice}+";
    }

    /// <summary>
    /// Các giá trị bộ lọc người dùng đã chọn
    /// </summary>
    public class ShopFilterVM
    {
        /// <summary>
        /// ID danh mục lá đã chọn. Sản phẩm sẽ gồm danh mục này và tất cả con cháu.
        /// </summary>
        public int? SelectedCategoryId { get; set; }

        public List<int> AttributeValueIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Keyword { get; set; }
    }
}
