using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;

namespace SmartShop.Areas.Customers.Controllers
{
    [Area("Customers")]
    public class ShopController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ShopController> _logger;

        public ShopController(IUnitOfWork unitOfWork, ILogger<ShopController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
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

            List<int> descendantCatIds = new();
            List<Category>? allActiveCats = null;

            if (filter.SelectedCategoryId.HasValue)
            {
                allActiveCats = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();

                descendantCatIds = GetDescendantIds(allActiveCats, filter.SelectedCategoryId.Value);

                vm.SelectedCategoryBreadcrumb = BuildBreadcrumb(allActiveCats, filter.SelectedCategoryId.Value)
                    .Select(c => new CategoryBreadcrumbItemVM { Id = c.Id, Name = c.Name, Slug = c.Slug })
                    .ToList();
            }

            var baseQ = _unitOfWork.Products.Query(p => p.IsActive);

            if (descendantCatIds.Any())
                baseQ = baseQ.Where(p =>
                    p.ProductCategories.Any(pc => descendantCatIds.Contains(pc.CategoryId)));

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
                baseQ = baseQ.Where(p => p.Name.Contains(filter.Keyword));

            if (filter.MinPrice.HasValue)
                baseQ = baseQ.Where(p => (p.SalePrice ?? p.BasePrice) >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                baseQ = baseQ.Where(p => (p.SalePrice ?? p.BasePrice) <= filter.MaxPrice.Value);

            var filteredQ = baseQ;
            var activeAvIds = filter.AttributeValueIds.Where(id => id > 0).ToList();
            if (activeAvIds.Any())
            {
                var valueToAttrMap = await _unitOfWork.AttributeValues.Query()
                    .Where(v => activeAvIds.Contains(v.Id))
                    .Select(v => new { v.Id, v.AttributeId })
                    .ToListAsync();

                var valueIdsByAttr = valueToAttrMap
                    .GroupBy(x => x.AttributeId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

                foreach (var kvp in valueIdsByAttr)
                {
                    var groupValueIds = kvp.Value;
                    filteredQ = filteredQ.Where(p =>
                        p.ProductAttributeValues.Any(pav => groupValueIds.Contains(pav.AttributeValueId)));
                }
            }

            if (filter.SelectedCategoryId.HasValue)
            {
                var contextIds = (await filteredQ.Select(p => p.Id).ToListAsync()).ToHashSet();
                vm.Attributes = await LoadShopAttributes(
                    filter.SelectedCategoryId.Value,
                    allActiveCats!,
                    contextIds,
                    filter.AttributeValueIds);
            }

            filteredQ = sortBy switch
            {
                "price-asc"  => filteredQ.OrderBy(p => (p.SalePrice ?? p.BasePrice)),
                "price-desc" => filteredQ.OrderByDescending(p => (p.SalePrice ?? p.BasePrice)),
                "popular"    => filteredQ.OrderByDescending(p => p.IsFeatured),
                _            => filteredQ.OrderByDescending(p => p.CreatedAt)
            };

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

            var allActiveQ = _unitOfWork.Products.Query(p => p.IsActive);
            vm.PriceRanges = new List<PriceRangeVM>
            {
                new() { MinPrice = 0,         MaxPrice = 500_000,   Label = "Dưới 500.000₫",
                        ProductCount = allActiveQ.Count(p => (p.SalePrice ?? p.BasePrice) <= 500_000),
                        IsSelected   = filter.MinPrice == 0 && filter.MaxPrice == 500_000 },
                new() { MinPrice = 500_000,   MaxPrice = 1_000_000, Label = "500.000₫ – 1.000.000₫",
                        ProductCount = allActiveQ.Count(p => (p.SalePrice ?? p.BasePrice) > 500_000 && (p.SalePrice ?? p.BasePrice) <= 1_000_000),
                        IsSelected   = filter.MinPrice == 500_000 && filter.MaxPrice == 1_000_000 },
                new() { MinPrice = 1_000_000, MaxPrice = 2_000_000, Label = "1.000.000₫ – 2.000.000₫",
                        ProductCount = allActiveQ.Count(p => (p.SalePrice ?? p.BasePrice) > 1_000_000 && (p.SalePrice ?? p.BasePrice) <= 2_000_000),
                        IsSelected   = filter.MinPrice == 1_000_000 && filter.MaxPrice == 2_000_000 },
                new() { MinPrice = 2_000_000, MaxPrice = null,      Label = "Trên 2.000.000₫",
                        ProductCount = allActiveQ.Count(p => (p.SalePrice ?? p.BasePrice) > 2_000_000),
                        IsSelected   = filter.MinPrice == 2_000_000 && filter.MaxPrice == null }
            };

            bool isPartial = Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || Request.Query["partial"] == "1";
            if (isPartial)
                return PartialView("_ShopAjaxPartial", vm);

            return View(vm);
        }

        // GET: /Customers/Shop/Detail/{slug}
        public async Task<IActionResult> Detail(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var product = await _unitOfWork.Products.GetBySlugAsync(slug);
            if (product == null)
                return NotFound();

            var soldCount = await _unitOfWork.OrderItems
                .Query(oi => oi.ProductId == product.Id)
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            var attributeGroups = product.ProductAttributeValues
                .GroupBy(pav => pav.AttributeValue.Attribute,
                         (attr, pavs) => new ProductAttributeGroupVM
                         {
                             AttributeName        = attr.Name,
                             AttributeDisplayName = attr.DisplayName,
                             Values = pavs
                                 .OrderBy(pav => pav.AttributeValue.SortOrder)
                                 .Select(pav => pav.AttributeValue.DisplayName)
                                 .ToList()
                         })
                .OrderBy(g => g.AttributeName)
                .ToList();

            var categoryId = product.ProductCategories.FirstOrDefault()?.CategoryId;
            var relatedProducts = new List<ProductItemVM>();
            if (categoryId.HasValue)
            {
                relatedProducts = await _unitOfWork.Products
                    .Query(p => p.IsActive
                             && p.Id != product.Id
                             && p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value))
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(4)
                    .Select(p => new ProductItemVM
                    {
                        Id           = p.Id,
                        Name         = p.Name,
                        Slug         = p.Slug,
                        ThumbnailUrl = p.ThumbnailUrl,
                        BasePrice    = p.BasePrice,
                        SalePrice    = p.SalePrice,
                        Stock        = p.Stock,
                        IsFeatured   = p.IsFeatured
                    })
                    .ToListAsync();
            }

            var vm = new ProductDetailVM
            {
                Id              = product.Id,
                Name            = product.Name,
                Slug            = product.Slug,
                ThumbnailUrl    = product.ThumbnailUrl,
                BasePrice       = product.BasePrice,
                SalePrice       = product.SalePrice,
                Description     = product.Description,
                Stock           = product.Stock,
                IsFeatured      = product.IsFeatured,
                SoldCount       = soldCount,
                AttributeGroups = attributeGroups,
                RelatedProducts = relatedProducts
            };

            return View(vm);
        }

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

        private async Task<List<AttributeFilterVM>> LoadShopAttributes(
            int categoryId,
            List<Category> allActiveCats,
            HashSet<int> baseProductIds,
            List<int> selectedValueIds)
        {
            var ancestorIds = GetAncestorIds(allActiveCats, categoryId);

            var catAttrs = await _unitOfWork.CategoryAttributes.Query()
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

            if (!catAttrs.Any())
                return new List<AttributeFilterVM>();

            var uniqueAttrs = catAttrs
                .GroupBy(ca => ca.AttributeId)
                .Select(g => g.OrderBy(ca => ancestorIds.IndexOf(ca.CategoryId)).First())
                .OrderBy(ca => ca.SortOrder)
                .ThenBy(ca => ca.Attribute.DisplayName)
                .ToList();

            var allValueIds = uniqueAttrs
                .SelectMany(ca => ca.Attribute.Values.Select(v => v.Id))
                .ToList();

            var countsByValueId = await _unitOfWork.ProductAttributeValues.Query()
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
