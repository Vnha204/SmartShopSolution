using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class ProductCategoryRepository : Repository<ProductCategory>, IProductCategoryRepository
    {
        public ProductCategoryRepository(AppDbContext context) : base(context) { }
    }
}
