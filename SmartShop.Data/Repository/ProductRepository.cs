using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShop.Data.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(SmartShopDbContext context) : base(context) { }

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

            // Lọc giá — NULL-safe (dùng SalePrice nếu có, ngược lại dùng BasePrice)
            if (minPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= maxPrice.Value);

            // Dynamic filter — AND giữa các attribute, OR trong cùng attribute
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
    }
}
