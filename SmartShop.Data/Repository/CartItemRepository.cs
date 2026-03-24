using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(AppDbContext context) : base(context) { }

        public async Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId)
            => await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cartId && ci.ProductId == productId);

        public async Task<IEnumerable<CartItem>> GetByCartIdAsync(int cartId)
            => await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();
    }
}
