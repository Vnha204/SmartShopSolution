namespace SmartShop.Models.ViewModels
{
    public class CartViewModel
    {
        public int CartId { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }

    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal Subtotal => UnitPriceSnapshot * Quantity;
    }
}
