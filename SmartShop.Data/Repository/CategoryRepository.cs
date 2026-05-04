using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;


namespace SmartShop.Data.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context) { }

        public async Task<Category?> GetBySlugAsync(string slug)
            => await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);
    }
}
