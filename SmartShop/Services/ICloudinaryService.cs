using Microsoft.AspNetCore.Http;

namespace SmartShop.Services
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads <paramref name="file"/> to Cloudinary under <paramref name="folder"/>
        /// and returns the secure URL and public_id.
        /// </summary>
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder = "smartshop-products");

        /// <summary>
        /// Deletes the Cloudinary asset identified by <paramref name="publicId"/>.
        /// No-ops silently when publicId is null/empty.
        /// </summary>
        Task DeleteImageAsync(string? publicId);
    }
}
