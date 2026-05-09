using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface IPasswordResetOtpRepository : IRepository<PasswordResetOtp>
    {
        Task<PasswordResetOtp?> GetValidOtpAsync(string userId, string code);
    }
}
