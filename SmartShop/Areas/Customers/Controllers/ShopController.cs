using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;
using SmartShop.Models.ViewModels;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class ShopController : Controller
    {
        private readonly AppDbContext _db;

        public ShopController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /Customers/Shop
        public async Task<IActionResult> Index([FromQuery] ShopFilterVM filter, string sortBy = "newest", int page = 1, int pageSize = 12)
        {
            var vm = new ShopViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                SortBy = sortBy
            };

            // Base query
            var query = _db.Products
                .Where(p => p.IsActive)
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .AsQueryable();

            // Filter by categories
            if (filter?.CategoryIds != null && filter.CategoryIds.Any())
            {
                query = query.Where(p => p.ProductCategories.Any(pc => filter.CategoryIds.Contains(pc.CategoryId)));
                vm.Filter.CategoryIds = filter.CategoryIds;
            }

            // Filter by attribute values
            if (filter?.AttributeValueIds != null && filter.AttributeValueIds.Any())
            {
                query = query.Where(p => p.ProductAttributeValues.Any(av => filter.AttributeValueIds.Contains(av.AttributeValueId)));
                vm.Filter.AttributeValueIds = filter.AttributeValueIds;
            }

            // Price filter
            if (filter?.MinPrice != null)
            {
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= filter.MinPrice.Value);
                vm.Filter.MinPrice = filter.MinPrice;
            }
            if (filter?.MaxPrice != null)
            {
                query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= filter.MaxPrice.Value);
                vm.Filter.MaxPrice = filter.MaxPrice;
            }

            // Keyword (if implemented)
            if (!string.IsNullOrWhiteSpace(filter?.Keyword))
            {
                query = query.Where(p => p.Name.Contains(filter.Keyword));
                vm.Filter.Keyword = filter.Keyword;
            }

            // Sorting
            query = sortBy switch
            {
                "price-asc" => query.OrderBy(p => (p.SalePrice ?? p.BasePrice)),
                "price-desc" => query.OrderByDescending(p => (p.SalePrice ?? p.BasePrice)),
                "popular" => query.OrderByDescending(p => p.IsFeatured),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // Count and pagination
            var total = await query.CountAsync();
            vm.TotalProducts = total;
            vm.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            vm.CurrentPage = Math.Clamp(page, 1, Math.Max(1, vm.TotalPages));

            var products = await query
                .Skip((vm.CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            vm.Products = products.Select(p => new ProductItemVM
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ThumbnailUrl = p.ThumbnailUrl,
                BasePrice = p.BasePrice,
                SalePrice = p.SalePrice,
                Stock = p.Stock,
                IsFeatured = p.IsFeatured
            }).ToList();

            // Build category filter data
            var categories = await _db.Categories
                .Select(c => new CategoryFilterVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = _db.ProductCategories.Count(pc => pc.CategoryId == c.Id)
                }).ToListAsync();

            // Mark selected categories
            foreach (var cat in categories)
            {
                if (vm.Filter.CategoryIds.Contains(cat.Id)) cat.IsSelected = true;
            }

            vm.Categories = categories;

            // Build simple attributes - fetch Attributes and values with product counts
            var attributes = await _db.Attributes
                .Include(a => a.Values)
                .ToListAsync();

            vm.Attributes = attributes.Select(a => new AttributeFilterVM
            {
                Id = a.Id,
                Name = a.Name,
                DisplayName = a.DisplayName ?? a.Name,
                Values = a.Values.Select(v => new AttributeValueFilterVM
                {
                    Id = v.Id,
                    Value = v.Value,
                    ProductCount = _db.ProductAttributeValues.Count(pav => pav.AttributeValueId == v.Id),
                    IsSelected = vm.Filter.AttributeValueIds.Contains(v.Id)
                }).ToList()
            }).ToList();

            // Price ranges (example presets)
            vm.PriceRanges = new List<PriceRangeVM>
            {
                new PriceRangeVM { MinPrice = 0, MaxPrice = 500000, Label = "Dưới 500.000₫", ProductCount = _db.Products.Count(p => (p.SalePrice ?? p.BasePrice) <= 500000) },
                new PriceRangeVM { MinPrice = 500000, MaxPrice = 1000000, Label = "500.000₫ - 1.000.000₫", ProductCount = _db.Products.Count(p => (p.SalePrice ?? p.BasePrice) > 500000 && (p.SalePrice ?? p.BasePrice) <= 1000000) },
                new PriceRangeVM { MinPrice = 1000000, MaxPrice = 2000000, Label = "1.000.000₫ - 2.000.000₫", ProductCount = _db.Products.Count(p => (p.SalePrice ?? p.BasePrice) > 1000000 && (p.SalePrice ?? p.BasePrice) <= 2000000) },
                new PriceRangeVM { MinPrice = 2000000, MaxPrice = null, Label = "Trên 2.000.000₫", ProductCount = _db.Products.Count(p => (p.SalePrice ?? p.BasePrice) > 2000000) }
            };

            vm.SortBy = sortBy;

            return View(vm);
        }
    }
}