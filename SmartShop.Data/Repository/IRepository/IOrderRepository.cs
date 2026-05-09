using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
        Task<Order?> GetWithDetailsAsync(int id);
        Task<IEnumerable<Order>> GetAllWithDetailsAsync();
        Task<IEnumerable<Order>> GetOrdersByAddressIdAsync(int addressId);
    }
}
