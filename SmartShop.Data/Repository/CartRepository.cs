using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }

        public async Task<Cart?> GetByUserIdAsync(string userId)
            => await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Cart?> GetWithItemsAsync(string userId)
            => await _context.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
    }
}
