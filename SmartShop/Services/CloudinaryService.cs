using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace SmartShop.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]);

            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<(string Url, string PublicId)> UploadImageAsync(
            IFormFile file, string folder = "smartshop-products")
        {
            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File         = new FileDescription(file.FileName, stream),
                Folder       = folder,
                UseFilename  = false,
                UniqueFilename = true,
                Overwrite    = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException(
                    $"Cloudinary upload failed: {result.Error.Message}");

            return (result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task DeleteImageAsync(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;

            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
