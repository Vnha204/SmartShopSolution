using Microsoft.AspNetCore.Http;

namespace SmartShop.Services
{
    public interface ICloudinaryService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder = "smartshop-products");
        Task DeleteImageAsync(string? publicId);
    }
}
