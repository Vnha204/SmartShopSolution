namespace SmartShop.Models.ViewModels
{
    public class ShopViewModel
    {
        public List<ProductItemVM> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public List<AttributeFilterVM> Attributes { get; set; } = new();
        public List<PriceRangeVM> PriceRanges { get; set; } = new();
        public List<CategoryBreadcrumbItemVM> SelectedCategoryBreadcrumb { get; set; } = new();
        public ShopFilterVM Filter { get; set; } = new();
        public string SortBy { get; set; } = "newest";
    }
    public class CategoryBreadcrumbItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }
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
    public class AttributeFilterVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<AttributeValueFilterVM> Values { get; set; } = new();
    }
    public class AttributeValueFilterVM
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;        
        public string DisplayName { get; set; } = string.Empty; 
        public int ProductCount { get; set; }
        public bool IsSelected { get; set; }
    }
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
    public class ShopFilterVM
    {
        public int? SelectedCategoryId { get; set; }

        public List<int> AttributeValueIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Keyword { get; set; }
    }
}
