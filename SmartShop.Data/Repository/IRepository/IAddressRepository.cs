using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IEnumerable<Address>> GetByUserIdAsync(string userId);
        Task<bool> IsUsedInOrderAsync(int addressId);
    }
}
