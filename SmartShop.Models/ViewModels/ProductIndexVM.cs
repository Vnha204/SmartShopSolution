namespace SmartShop.Models.ViewModels
{

    public class ProductIndexVM
    {
        public string? SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all";
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public List<int> AttributeValueIds { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Category> AllCategories { get; set; } = new();
        public List<AttributeGroupVM> FilterAttributeGroups { get; set; } = new();
        public int TotalCount => Products.Count;
        public bool HasActiveFilter =>
            !string.IsNullOrWhiteSpace(SearchTerm) ||
            (StatusFilter != "all" && !string.IsNullOrWhiteSpace(StatusFilter)) ||
            CategoryId.HasValue ||
            MinPrice.HasValue ||
            MaxPrice.HasValue ||
            AttributeValueIds.Any(id => id > 0);
    }
}
