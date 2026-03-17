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
        public async Task<IActionResult> Index(
            [FromQuery] ShopFilterVM filter,
            string sortBy = "newest",
            int page = 1,
            int pageSize = 12)
        {
            filter ??= new ShopFilterVM();
            filter.AttributeValueIds ??= new List<int>();

            var vm = new ShopViewModel
            {
                CurrentPage = page,
                PageSize    = pageSize,
                SortBy      = sortBy,
                Filter      = filter
            };

            // ─────────────────────────────────────────────────────────────────
            // 1. Resolve category + all descendants
            // ─────────────────────────────────────────────────────────────────
            List<int> descendantCatIds = new();
            List<Category>? allActiveCats = null;

            if (filter.SelectedCategoryId.HasValue)
            {
                allActiveCats = await _db.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                descendantCatIds = GetDescendantIds(allActiveCats, filter.SelectedCategoryId.Value);

                vm.SelectedCategoryBreadcrumb = BuildBreadcrumb(allActiveCats, filter.SelectedCategoryId.Value)
                    .Select(c => new CategoryBreadcrumbItemVM { Id = c.Id, Name = c.Name, Slug = c.Slug })
                    .ToList();
            }

            // ─────────────────────────────────────────────────────────────────
            // 2. Base query — category descendants + keyword + price
            //    (attribute filter NOT applied here — used for attribute counts)
            // ─────────────────────────────────────────────────────────────────
            var baseQ = _db.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (descendantCatIds.Any())
                baseQ = baseQ.Where(p =>
                    p.ProductCategories.Any(pc => descendantCatIds.Contains(pc.CategoryId)));

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
                baseQ = baseQ.Where(p => p.Name.Contains(filter.Keyword));

            if (filter.MinPrice.HasValue)
                baseQ = baseQ.Where(p => (p.SalePrice ?? p.BasePrice) >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                baseQ = baseQ.Where(p => (p.SalePrice ?? p.BasePrice) <= filter.MaxPrice.Value);

            // ─────────────────────────────────────────────────────────────────
            // 3. Load dynamic attributes with counts from base query context
            // ─────────────────────────────────────────────────────────────────
            if (filter.SelectedCategoryId.HasValue)
            {
                var baseIds = (await baseQ.Select(p => p.Id).ToListAsync()).ToHashSet();
                vm.Attributes = await LoadShopAttributes(
                    filter.SelectedCategoryId.Value,
                    allActiveCats!,
                    baseIds,
                    filter.AttributeValueIds);
            }

            // ─────────────────────────────────────────────────────────────────
            // 4. Full filtered query — add attribute filter on top of base
            // ─────────────────────────────────────────────────────────────────
            var filteredQ = baseQ;
            var activeAvIds = filter.AttributeValueIds.Where(id => id > 0).ToList();
            if (activeAvIds.Any())
                filteredQ = filteredQ.Where(p =>
                    p.ProductAttributeValues.Any(pav => activeAvIds.Contains(pav.AttributeValueId)));

            // ─────────────────────────────────────────────────────────────────
            // 5. Sort
            // ─────────────────────────────────────────────────────────────────
            filteredQ = sortBy switch
            {
                "price-asc"  => filteredQ.OrderBy(p => (p.SalePrice ?? p.BasePrice)),
                "price-desc" => filteredQ.OrderByDescending(p => (p.SalePrice ?? p.BasePrice)),
                "popular"    => filteredQ.OrderByDescending(p => p.IsFeatured),
                _            => filteredQ.OrderByDescending(p => p.CreatedAt)
            };

            // ─────────────────────────────────────────────────────────────────
            // 6. Count + paginate
            // ─────────────────────────────────────────────────────────────────
            var total = await filteredQ.CountAsync();
            vm.TotalProducts = total;
            vm.TotalPages    = (int)Math.Ceiling(total / (double)pageSize);
            vm.CurrentPage   = Math.Clamp(page, 1, Math.Max(1, vm.TotalPages));

            var products = await filteredQ
                .Skip((vm.CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            vm.Products = products.Select(p => new ProductItemVM
            {
                Id           = p.Id,
                Name         = p.Name,
                Slug         = p.Slug,
                ThumbnailUrl = p.ThumbnailUrl,
                BasePrice    = p.BasePrice,
                SalePrice    = p.SalePrice,
                Stock        = p.Stock,
                IsFeatured   = p.IsFeatured
            }).ToList();

            // ─────────────────────────────────────────────────────────────────
            // 7. Price range presets (counted against all active products)
            // ─────────────────────────────────────────────────────────────────
            var allActive = _db.Products.Where(p => p.IsActive);
            vm.PriceRanges = new List<PriceRangeVM>
            {
                new() { MinPrice = 0,       MaxPrice = 500_000,   Label = "Dưới 500.000₫",
                        ProductCount = allActive.Count(p => (p.SalePrice ?? p.BasePrice) <= 500_000),
                        IsSelected   = filter.MinPrice == 0 && filter.MaxPrice == 500_000 },
                new() { MinPrice = 500_000, MaxPrice = 1_000_000, Label = "500.000₫ – 1.000.000₫",
                        ProductCount = allActive.Count(p => (p.SalePrice ?? p.BasePrice) > 500_000 && (p.SalePrice ?? p.BasePrice) <= 1_000_000),
                        IsSelected   = filter.MinPrice == 500_000 && filter.MaxPrice == 1_000_000 },
                new() { MinPrice = 1_000_000, MaxPrice = 2_000_000, Label = "1.000.000₫ – 2.000.000₫",
                        ProductCount = allActive.Count(p => (p.SalePrice ?? p.BasePrice) > 1_000_000 && (p.SalePrice ?? p.BasePrice) <= 2_000_000),
                        IsSelected   = filter.MinPrice == 1_000_000 && filter.MaxPrice == 2_000_000 },
                new() { MinPrice = 2_000_000, MaxPrice = null,    Label = "Trên 2.000.000₫",
                        ProductCount = allActive.Count(p => (p.SalePrice ?? p.BasePrice) > 2_000_000),
                        IsSelected   = filter.MinPrice == 2_000_000 && filter.MaxPrice == null }
            };

            bool isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Query["partial"] == "1";
            if (isPartial)
                return PartialView("_ShopAjaxPartial", vm);

            return View(vm);
        }

        // =====================================================================
        // Private helpers
        // =====================================================================

        /// <summary>
        /// BFS from <paramref name="categoryId"/> downwards; returns the given ID
        /// plus all descendant IDs.
        /// </summary>
        private static List<int> GetDescendantIds(List<Category> allCats, int categoryId)
        {
            var result = new List<int>();
            var queue  = new Queue<int>();
            queue.Enqueue(categoryId);

            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                result.Add(id);
                foreach (var child in allCats.Where(c => c.ParentId == id))
                    queue.Enqueue(child.Id);
            }

            return result;
        }

        /// <summary>
        /// Returns [root, …, leaf] breadcrumb for the given category.
        /// </summary>
        private static List<Category> BuildBreadcrumb(List<Category> allCats, int categoryId)
        {
            var chain   = new List<Category>();
            var current = allCats.FirstOrDefault(c => c.Id == categoryId);
            while (current != null)
            {
                chain.Insert(0, current);
                current = current.ParentId.HasValue
                    ? allCats.FirstOrDefault(c => c.Id == current.ParentId.Value)
                    : null;
            }
            return chain;
        }

        /// <summary>
        /// Returns [leaf, parent, …, root] ancestor chain for attribute lookup.
        /// </summary>
        private static List<int> GetAncestorIds(List<Category> allCats, int categoryId)
        {
            var ids     = new List<int>();
            var current = allCats.FirstOrDefault(c => c.Id == categoryId);
            while (current != null)
            {
                ids.Add(current.Id);
                current = current.ParentId.HasValue
                    ? allCats.FirstOrDefault(c => c.Id == current.ParentId.Value)
                    : null;
            }
            return ids;
        }

        /// <summary>
        /// Loads attribute groups applicable to <paramref name="categoryId"/> (including
        /// ancestors) with per-value product counts computed within
        /// <paramref name="baseProductIds"/> (the pre-attribute-filtered context).
        /// </summary>
        private async Task<List<AttributeFilterVM>> LoadShopAttributes(
            int categoryId,
            List<Category> allActiveCats,
            HashSet<int> baseProductIds,
            List<int> selectedValueIds)
        {
            var ancestorIds = GetAncestorIds(allActiveCats, categoryId);

            var catAttrs = await _db.CategoryAttributes
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

            if (!catAttrs.Any())
                return new List<AttributeFilterVM>();

            // De-duplicate: closest to leaf wins
            var uniqueAttrs = catAttrs
                .GroupBy(ca => ca.AttributeId)
                .Select(g => g.OrderBy(ca => ancestorIds.IndexOf(ca.CategoryId)).First())
                .OrderBy(ca => ca.SortOrder)
                .ThenBy(ca => ca.Attribute.DisplayName)
                .ToList();

            // Collect all relevant value IDs so we can do one count query
            var allValueIds = uniqueAttrs
                .SelectMany(ca => ca.Attribute.Values.Select(v => v.Id))
                .ToList();

            // Count products (within base context) per attribute value in one DB call
            var countsByValueId = await _db.ProductAttributeValues
                .Where(pav => allValueIds.Contains(pav.AttributeValueId)
                           && baseProductIds.Contains(pav.ProductId))
                .GroupBy(pav => pav.AttributeValueId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var countDict = countsByValueId.ToDictionary(x => x.Id, x => x.Count);

            return uniqueAttrs.Select(ca => new AttributeFilterVM
            {
                Id          = ca.AttributeId,
                Name        = ca.Attribute.Name,
                DisplayName = ca.Attribute.DisplayName,
                Values = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.DisplayName)
                    .Select(v => new AttributeValueFilterVM
                    {
                        Id           = v.Id,
                        Value        = v.Value,
                        DisplayName  = v.DisplayName,
                        ProductCount = countDict.GetValueOrDefault(v.Id, 0),
                        IsSelected   = selectedValueIds.Contains(v.Id)
                    })
                    .ToList()
            }).ToList();
        }
    }
}
