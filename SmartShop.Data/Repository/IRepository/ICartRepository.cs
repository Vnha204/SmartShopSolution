using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface ICartRepository : IRepository<Cart>
    {
        Task<Cart?> GetByUserIdAsync(string userId);
        Task<Cart?> GetWithItemsAsync(string userId);
    }
}
