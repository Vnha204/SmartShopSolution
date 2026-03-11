using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(SmartShopDbContext context) : base(context) { }

        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
            => await _context.Categories
                .Where(c => c.ParentId == null && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

        public async Task<IEnumerable<Category>> GetChildrenAsync(int parentId)
            => await _context.Categories
                .Where(c => c.ParentId == parentId && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

        public async Task<Category?> GetWithAttributesAsync(int categoryId)
            => await _context.Categories
                .Include(c => c.CategoryAttributes)
                    .ThenInclude(ca => ca.Attribute)
                        .ThenInclude(a => a.Values)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

        public async Task<Category?> GetBySlugAsync(string slug)
            => await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);
    }
}
