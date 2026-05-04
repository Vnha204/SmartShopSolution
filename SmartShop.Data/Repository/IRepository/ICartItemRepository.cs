using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface ICartItemRepository : IRepository<CartItem>
    {
        Task<CartItem?> GetByCartAndProductAsync(int cartId, int productId);
    }
}
