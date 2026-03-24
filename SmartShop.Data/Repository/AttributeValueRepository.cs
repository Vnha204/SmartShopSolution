using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class AttributeValueRepository : Repository<AttributeValue>, IAttributeValueRepository
    {
        public AttributeValueRepository(AppDbContext context) : base(context) { }
    }
}
