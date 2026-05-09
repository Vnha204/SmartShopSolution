using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class PasswordResetOtpRepository : Repository<PasswordResetOtp>, IPasswordResetOtpRepository
    {
        public PasswordResetOtpRepository(AppDbContext context) : base(context) { }

        public async Task<PasswordResetOtp?> GetValidOtpAsync(string userId, string code)
            => await _context.PasswordResetOtps
                .Where(o => o.UserId == userId
                         && o.OtpCode == code
                         && !o.IsUsed
                         && o.OtpExpiry > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();
    }
}
