namespace SmartShop.Models
{
    /// <summary>
    /// Bound from configuration section "VietQR".
    /// Set real values via user-secrets or environment variables — never commit them.
    /// <code>
    ///   dotnet user-secrets set "VietQR:BankName"      "Vietcombank"
    ///   dotnet user-secrets set "VietQR:BankBin"       "970436"
    ///   dotnet user-secrets set "VietQR:AccountNumber" "1234567890"
    ///   dotnet user-secrets set "VietQR:AccountName"   "SHOP NAME"
    ///   dotnet user-secrets set "VietQR:ApiClientId"   "your-client-id"
    ///   dotnet user-secrets set "VietQR:ApiApiKey"     "your-api-key"
    /// </code>
    /// Or environment variables:
    ///   VietQR__BankName, VietQR__BankBin, VietQR__AccountNumber, VietQR__AccountName
    ///   VietQR__ApiBaseUrl, VietQR__ApiClientId, VietQR__ApiApiKey
    /// </summary>
    public class VietQRSettings
    {
        public string BankName      { get; set; } = string.Empty;
        public string BankBin       { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName   { get; set; } = string.Empty;
        public string ApiBaseUrl    { get; set; } = "https://api.vietqr.io";
        public string ApiClientId   { get; set; } = string.Empty;
        public string ApiApiKey     { get; set; } = string.Empty;
    }
}
