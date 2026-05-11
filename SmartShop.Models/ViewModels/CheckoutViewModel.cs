using SmartShop.Models;

namespace SmartShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(i => i.Subtotal);
        public List<Address> Addresses { get; set; } = new();
        public int? SelectedAddressId { get; set; }
        public string? NewFullName { get; set; }
        public string? NewPhone { get; set; }
        public string? NewProvince { get; set; }
        public string? NewDistrict { get; set; }
        public string? NewWard { get; set; }
        public string? NewStreetLine { get; set; }
        public string PaymentMethod { get; set; } = "cod";
        public string? Note { get; set; }
    }
}
