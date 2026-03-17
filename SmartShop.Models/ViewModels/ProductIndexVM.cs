namespace SmartShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Admin Product Index page.
    /// Carries both the filter parameters (bound from GET query string)
    /// and the result data (products + filter options) populated by the controller.
    /// </summary>
    public class ProductIndexVM
    {
        // ── Filter parameters (bound from query string) ──────────────────────
        public string? SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all";
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// One selected AttributeValue per attribute group (e.g. one Gender id, one Style id).
        /// Multiple query-string entries with the same key: ?AttributeValueIds=3&AttributeValueIds=7
        /// </summary>
        public List<int> AttributeValueIds { get; set; } = new();

        // ── Result data (populated by controller, NOT bound from form) ────────
        public List<Product> Products { get; set; } = new();

        // ── Filter-UI data (populated by controller) ──────────────────────────
        public List<Category> AllCategories { get; set; } = new();

        /// <summary>
        /// All attribute groups that have at least one CategoryAttribute mapping.
        /// Used to render the attribute filter dropdowns.
        /// </summary>
        public List<AttributeGroupVM> FilterAttributeGroups { get; set; } = new();

        // ── Computed helpers ──────────────────────────────────────────────────
        public int TotalCount => Products.Count;

        /// <summary>
        /// Returns true when any filter is active (used to highlight "Clear filters" button).
        /// </summary>
        public bool HasActiveFilter =>
            !string.IsNullOrWhiteSpace(SearchTerm) ||
            (StatusFilter != "all" && !string.IsNullOrWhiteSpace(StatusFilter)) ||
            CategoryId.HasValue ||
            MinPrice.HasValue ||
            MaxPrice.HasValue ||
            AttributeValueIds.Any(id => id > 0);
    }
}
