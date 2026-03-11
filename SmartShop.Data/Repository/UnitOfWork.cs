using SmartShop.Data.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository
{
    /// <summary>
    /// Unit of Work — gom tất cả repositories, dùng chung 1 DbContext.
    /// Inject IUnitOfWork vào Controller/Service thay vì inject từng repository riêng lẻ.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SmartShopDbContext _context;

        public ICategoryRepository Categories { get; private set; }
        public IProductRepository Products { get; private set; }
        //public IUserRepository Users { get; private set; }

        public UnitOfWork(SmartShopDbContext context)
        {
            _context = context;
            Categories = new CategoryRepository(_context);
            Products = new ProductRepository(_context);
            //Users = new UserRepository(_context);
        }

        public async Task<int> SaveAsync()
            => await _context.SaveChangesAsync();

        public void Dispose()
            => _context.Dispose();
    }
}
