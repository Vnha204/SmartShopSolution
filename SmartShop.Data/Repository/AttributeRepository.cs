using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using ModelAttribute = SmartShop.Models.Attribute;

namespace SmartShop.Data.Repository
{
    public class AttributeRepository : Repository<ModelAttribute>, IAttributeRepository
    {
        public AttributeRepository(AppDbContext context) : base(context) { }
    }
}
