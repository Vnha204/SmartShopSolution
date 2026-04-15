namespace SmartShop.Data.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }
        IOrderItemRepository OrderItems { get; }
        IAddressRepository Addresses { get; }
        IAttributeRepository Attributes { get; }
        IAttributeValueRepository AttributeValues { get; }
        ICategoryAttributeRepository CategoryAttributes { get; }
        IProductCategoryRepository ProductCategories { get; }
        IProductAttributeValueRepository ProductAttributeValues { get; }
        ICartRepository Carts { get; }
        ICartItemRepository CartItems { get; }
        IContactMessageRepository ContactMessages { get; }

        Task<int> SaveAsync();
    }
}
