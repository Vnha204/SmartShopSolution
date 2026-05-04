using SmartShop.Models;

namespace SmartShop.Data.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Task<Category?> GetBySlugAsync(string slug);
    }
}
