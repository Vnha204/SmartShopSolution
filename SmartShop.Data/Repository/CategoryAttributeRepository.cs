using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class CategoryAttributeRepository : Repository<CategoryAttribute>, ICategoryAttributeRepository
    {
        public CategoryAttributeRepository(AppDbContext context) : base(context) { }
    }
}
