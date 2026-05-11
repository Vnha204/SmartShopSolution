using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;

namespace SmartShop.Data.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext context) : base(context) { }

        public async Task<Product?> GetBySlugAsync(string slug)
            => await _context.Products
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.ProductAttributeValues).ThenInclude(pav => pav.AttributeValue)
                    .ThenInclude(av => av.Attribute)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        public async Task<IEnumerable<Product>> FilterAsync(
            int categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            List<int>? attributeValueIds)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId));

            if (minPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= maxPrice.Value);

            if (attributeValueIds != null && attributeValueIds.Any())
            {
                var grouped = await _context.AttributeValues
                    .Where(av => attributeValueIds.Contains(av.Id))
                    .GroupBy(av => av.AttributeId)
                    .Select(g => g.Select(av => av.Id).ToList())
                    .ToListAsync();

                foreach (var group in grouped)
                {
                    var ids = group;
                    query = query.Where(p =>
                        p.ProductAttributeValues.Any(pav => ids.Contains(pav.AttributeValueId)));
                }
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedAsync(int take = 8)
            => await _context.Products
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .ToListAsync();

        public async Task<Product?> GetWithFullDetailsAsync(int id)
            => await _context.Products
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.Attribute)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Product?> GetWithCategoriesAndAttributesAsync(int id)
            => await _context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id);
    }
}
