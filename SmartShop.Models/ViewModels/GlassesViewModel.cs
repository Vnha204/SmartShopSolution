namespace SmartShop.Models.ViewModels
{
    public class GlassesViewModel
    {
        public List<ProductItemVM> Products { get; set; } = new();
        public string ActiveFilter { get; set; } = "all";
    }
}
