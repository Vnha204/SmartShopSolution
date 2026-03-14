using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Shop - hiển thị danh sách sản phẩm với bộ lọc
    /// </summary>
    public class ShopViewModel
    {
        // ===== Danh sách sản phẩm =====
        public List<ProductItemVM> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // ===== Bộ lọc =====
        public List<CategoryFilterVM> Categories { get; set; } = new();
        public List<AttributeFilterVM> Attributes { get; set; } = new();
        public List<PriceRangeVM> PriceRanges { get; set; } = new();

        // ===== Giá trị bộ lọc đã chọn =====
        public ShopFilterVM Filter { get; set; } = new();
        
        // ===== Sắp xếp =====
        public string SortBy { get; set; } = "newest";
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

        /// <summary>
        /// Giá hiển thị (ưu tiên SalePrice nếu có)
        /// </summary>
        public decimal DisplayPrice => SalePrice ?? BasePrice;

        /// <summary>
        /// Có đang giảm giá không
        /// </summary>
        public bool IsOnSale => SalePrice.HasValue && SalePrice < BasePrice;

        /// <summary>
        /// Phần trăm giảm giá
        /// </summary>
        public int DiscountPercent => IsOnSale 
            ? (int)Math.Round((1 - (SalePrice!.Value / BasePrice)) * 100) 
            : 0;
    }

    /// <summary>
    /// Danh mục cho bộ lọc
    /// </summary>
    public class CategoryFilterVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// Thuộc tính (Attribute) cho bộ lọc - ví dụ: Kiểu gọng, Chất liệu, Giới tính
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
        public string Value { get; set; } = string.Empty;
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
        public List<int> CategoryIds { get; set; } = new();
        public List<int> AttributeValueIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Keyword { get; set; }
    }
}