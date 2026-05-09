using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
            => await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<Order?> GetWithDetailsAsync(int id)
            => await _context.Orders
                .IgnoreQueryFilters()
                .Include(o => o.User)
                .Include(o => o.Address)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<IEnumerable<Order>> GetAllWithDetailsAsync()
            => await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Order>> GetOrdersByAddressIdAsync(int addressId)
            => await _context.Orders
                .Where(o => o.AddressId == addressId)
                .ToListAsync();
    }
}
