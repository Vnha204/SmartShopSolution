using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class AddressRepository : Repository<Address>, IAddressRepository
    {
        public AddressRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(string userId)
            => await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToListAsync();

        public async Task<bool> IsUsedInOrderAsync(int addressId)
            => await _context.Orders.AnyAsync(o => o.AddressId == addressId);
    }
}
