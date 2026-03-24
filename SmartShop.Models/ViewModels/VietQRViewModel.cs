using SmartShop.Models;

namespace SmartShop.Models.ViewModels
{
    public class VietQRViewModel
    {
        public Order Order           { get; set; } = null!;
        public string BankName       { get; set; } = string.Empty;
        public string AccountNumber  { get; set; } = string.Empty;
        public string AccountName    { get; set; } = string.Empty;
        public string TransferContent { get; set; } = string.Empty;
    }
}
