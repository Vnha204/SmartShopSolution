namespace SmartShop.Services
{
    public interface IVietQrApiClient
    {
        /// <summary>
        /// Calls the VietQR API and returns a base64 data URL (data:image/png;base64,...).
        /// </summary>
        Task<string> GetQrDataUrlAsync(
            string bankBin,
            string accountNo,
            string accountName,
            long   amount,
            string addInfo);
    }
}
