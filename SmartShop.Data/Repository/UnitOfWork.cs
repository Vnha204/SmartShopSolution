using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;

namespace SmartShop.Data.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public ICategoryRepository Categories { get; private set; }
        public IProductRepository Products { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public IOrderItemRepository OrderItems { get; private set; }
        public IAddressRepository Addresses { get; private set; }
        public IAttributeRepository Attributes { get; private set; }
        public IAttributeValueRepository AttributeValues { get; private set; }
        public ICategoryAttributeRepository CategoryAttributes { get; private set; }
        public IProductCategoryRepository ProductCategories { get; private set; }
        public IProductAttributeValueRepository ProductAttributeValues { get; private set; }
        public ICartRepository Carts { get; private set; }
        public ICartItemRepository CartItems { get; private set; }

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Categories = new CategoryRepository(_context);
            Products = new ProductRepository(_context);
            Orders = new OrderRepository(_context);
            OrderItems = new OrderItemRepository(_context);
            Addresses = new AddressRepository(_context);
            Attributes = new AttributeRepository(_context);
            AttributeValues = new AttributeValueRepository(_context);
            CategoryAttributes = new CategoryAttributeRepository(_context);
            ProductCategories = new ProductCategoryRepository(_context);
            ProductAttributeValues = new ProductAttributeValueRepository(_context);
            Carts = new CartRepository(_context);
            CartItems = new CartItemRepository(_context);
        }

        public async Task<int> SaveAsync()
            => await _context.SaveChangesAsync();

        public void Dispose()
            => _context.Dispose();
    }
}
