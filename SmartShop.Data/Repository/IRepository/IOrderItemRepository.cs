using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface IOrderItemRepository : IRepository<OrderItem>
    {
        Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId);
    }
}
