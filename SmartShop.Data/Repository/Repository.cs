using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository
{
    /// <summary>
    /// Triển khai generic repository — dùng chung cho mọi entity.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly SmartShopDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private SmartShopDbContext context;

        public Repository(SmartShopDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }


        public async Task<T?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<T> entities)
            => await _dbSet.AddRangeAsync(entities);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void Remove(T entity)
            => _dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities)
            => _dbSet.RemoveRange(entities);
    }
}
