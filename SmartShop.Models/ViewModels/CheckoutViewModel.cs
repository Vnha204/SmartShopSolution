using SmartShop.Models;

namespace SmartShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // Cart summary (read-only display)
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);

        // Address selection
        public List<Address> Addresses { get; set; } = new();
        public int? SelectedAddressId { get; set; }

        // Inline new-address fields (used when SelectedAddressId is null/0)
        public string? NewFullName { get; set; }
        public string? NewPhone { get; set; }
        public string? NewProvince { get; set; }
        public string? NewDistrict { get; set; }
        public string? NewWard { get; set; }
        public string? NewStreetLine { get; set; }

        // Order options
        public string PaymentMethod { get; set; } = "cod";
        public string? Note { get; set; }
    }
}
