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
        /// <summary>Base64 data URL from VietQR API (data:image/png;base64,...). Null if API call failed.</summary>
        public string? QrDataUrl      { get; set; }
    }
}
