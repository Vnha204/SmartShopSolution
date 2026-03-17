using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartShop.Models.ViewModels
{
    public class ProductFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [MaxLength(300)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slug là bắt buộc")]
        [MaxLength(300)]
        [Display(Name = "Slug (URL)")]
        public string Slug { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá gốc là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá gốc (₫)")]
        public decimal BasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá khuyến mãi (₫)")]
        public decimal? SalePrice { get; set; }

        // Stored Cloudinary URL — populated by controller after upload, not a user input
        public string? ThumbnailUrl { get; set; }

        // File upload from form (Create: required; Edit: optional — leave blank to keep existing)
        [Display(Name = "Ảnh sản phẩm")]
        public IFormFile? ThumbnailFile { get; set; }

        [Range(0, int.MaxValue)]
        [Display(Name = "Tồn kho")]
        public int Stock { get; set; } = 0;

        [Display(Name = "Đang bán")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Nổi bật")]
        public bool IsFeatured { get; set; } = false;

        // Primary category (single dropdown — drives dynamic attributes via AJAX)
        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        // One AttributeValue per Attribute group (empty/0 means "not selected")
        public List<int> SelectedAttributeValueIds { get; set; } = new();

        // Populated by controller — not bound from form
        public List<Category> AllCategories { get; set; } = new();
        public List<AttributeGroupVM> AvailableAttributes { get; set; } = new();
    }

    /// <summary>
    /// One Attribute (e.g. "Giới tính") with all its possible values.
    /// Rendered as a single dropdown in the product form.
    /// </summary>
    public class AttributeGroupVM
    {
        public int AttributeId { get; set; }
        public string Name { get; set; } = string.Empty;         // internal key, e.g. "gender"
        public string DisplayName { get; set; } = string.Empty;  // UI label, e.g. "Giới tính"
        public int SortOrder { get; set; }
        public List<AttributeValueOptionVM> Options { get; set; } = new();
    }

    /// <summary>
    /// One selectable value inside an AttributeGroupVM dropdown.
    /// </summary>
    public class AttributeValueOptionVM
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
