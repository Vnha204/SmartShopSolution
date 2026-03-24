using QRCoder;

namespace SmartShop.Services
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GeneratePng(string payload, int pixelsPerModule = 8)
        {
            using var generator = new QRCodeGenerator();
            using var data      = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var code      = new PngByteQRCode(data);
            return code.GetGraphic(pixelsPerModule);
        }
    }
}
