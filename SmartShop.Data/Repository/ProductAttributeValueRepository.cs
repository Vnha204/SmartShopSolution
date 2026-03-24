using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class ProductAttributeValueRepository : Repository<ProductAttributeValue>, IProductAttributeValueRepository
    {
        public ProductAttributeValueRepository(AppDbContext context) : base(context) { }
    }
}
