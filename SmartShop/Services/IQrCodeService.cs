namespace SmartShop.Services
{
    public interface IQrCodeService
    {
        /// <summary>Returns a PNG byte array for the given text payload.</summary>
        byte[] GeneratePng(string payload, int pixelsPerModule = 8);
    }
}
