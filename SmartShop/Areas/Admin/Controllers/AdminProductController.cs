using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        public ProductController(AppDbContext context) => _context = context;

        // =====================================================================
        // GET: Admin/Product
        // Server-side filtering: name, status, category, attributes, price range
        // =====================================================================
        public async Task<IActionResult> Index(ProductIndexVM filter)
        {
            var query = _context.Products
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.ProductAttributeValues).ThenInclude(pav => pav.AttributeValue)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(p => p.Name.Contains(filter.SearchTerm));

            if (filter.StatusFilter == "active")
                query = query.Where(p => p.IsActive);
            else if (filter.StatusFilter == "inactive")
                query = query.Where(p => !p.IsActive);

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == filter.CategoryId.Value));

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);

            var activeAttrIds = filter.AttributeValueIds.Where(id => id > 0).ToList();
            foreach (var avId in activeAttrIds)
                query = query.Where(p => p.ProductAttributeValues.Any(pav => pav.AttributeValueId == avId));

            filter.Products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            filter.AllCategories = await _context.Categories
                .Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            filter.FilterAttributeGroups = await LoadFilterAttributeGroups();

            return View(filter);
        }

        // =====================================================================
        // AJAX — GET /Admin/Product/GetAttributesByCategory?categoryId=X
        // Legacy endpoint kept for backward compat; now includes ancestor attrs.
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetAttributesByCategory(int categoryId)
        {
            var groups = await LoadAttributeGroups(categoryId);
            var result = groups.Select(g => new
            {
                attributeId = g.AttributeId,
                name        = g.Name,
                displayName = g.DisplayName,
                sortOrder   = g.SortOrder,
                options     = g.Options.Select(o => new { id = o.Id, displayName = o.DisplayName })
            });
            return Json(result);
        }

        // =====================================================================
        // GET: Admin/Product/Details/5
        // =====================================================================
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.AttributeValue)
                        .ThenInclude(av => av.Attribute)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // =====================================================================
        // GET: Admin/Product/Create
        // =====================================================================
        public IActionResult Create()
        {
            // No AllCategories needed — drill-down uses /Admin/api/categories/*
            return View(new ProductFormVM());
        }

        // =====================================================================
        // POST: Admin/Product/Create
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormVM vm)
        {
            if (await _context.Products.AnyAsync(p => p.Slug == vm.Slug))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            // Category must be a leaf (no active children)
            if (vm.CategoryId.HasValue)
            {
                var hasChildren = await _context.Categories
                    .AnyAsync(c => c.ParentId == vm.CategoryId.Value && c.IsActive);
                if (hasChildren)
                    ModelState.AddModelError("CategoryId",
                        "Vui lòng chọn danh mục cấp cuối (danh mục không có danh mục con).");
            }

            // Validate attribute values belong to applicable attributes
            if (vm.CategoryId.HasValue && vm.SelectedAttributeValueIds.Any(id => id > 0))
                await ValidateAttributeValues(vm);

            if (!ModelState.IsValid)
            {
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                return View(vm);
            }

            var product = new Product
            {
                Name         = vm.Name,
                Slug         = vm.Slug.Trim().ToLower(),
                Description  = vm.Description,
                BasePrice    = vm.BasePrice,
                SalePrice    = vm.SalePrice,
                ThumbnailUrl = vm.ThumbnailUrl,
                Stock        = vm.Stock,
                IsActive     = vm.IsActive,
                IsFeatured   = vm.IsFeatured,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (vm.CategoryId.HasValue)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId  = product.Id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            foreach (var avId in vm.SelectedAttributeValueIds.Where(id => id > 0).Distinct())
            {
                _context.ProductAttributeValues.Add(new ProductAttributeValue
                {
                    ProductId        = product.Id,
                    AttributeValueId = avId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được thêm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // GET: Admin/Product/Edit/5
        // =====================================================================
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var primaryCategoryId = product.ProductCategories
                .Select(pc => pc.CategoryId)
                .FirstOrDefault();

            var vm = new ProductFormVM
            {
                Id           = product.Id,
                Name         = product.Name,
                Slug         = product.Slug,
                Description  = product.Description,
                BasePrice    = product.BasePrice,
                SalePrice    = product.SalePrice,
                ThumbnailUrl = product.ThumbnailUrl,
                Stock        = product.Stock,
                IsActive     = product.IsActive,
                IsFeatured   = product.IsFeatured,
                CategoryId   = primaryCategoryId > 0 ? primaryCategoryId : null,
                SelectedAttributeValueIds = product.ProductAttributeValues
                    .Select(pav => pav.AttributeValueId).ToList()
            };

            // Pre-load attribute groups (with ancestor union) for server-side render
            if (vm.CategoryId.HasValue)
                vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);

            return View(vm);
        }

        // =====================================================================
        // POST: Admin/Product/Edit/5
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormVM vm)
        {
            if (id != vm.Id) return BadRequest();

            if (await _context.Products.AnyAsync(p => p.Slug == vm.Slug && p.Id != id))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            // Category must be a leaf
            if (vm.CategoryId.HasValue)
            {
                var hasChildren = await _context.Categories
                    .AnyAsync(c => c.ParentId == vm.CategoryId.Value && c.IsActive);
                if (hasChildren)
                    ModelState.AddModelError("CategoryId",
                        "Vui lòng chọn danh mục cấp cuối (danh mục không có danh mục con).");
            }

            // Validate attribute values
            if (vm.CategoryId.HasValue && vm.SelectedAttributeValueIds.Any(id2 => id2 > 0))
                await ValidateAttributeValues(vm);

            if (!ModelState.IsValid)
            {
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                return View(vm);
            }

            var product = await _context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            product.Name         = vm.Name;
            product.Slug         = vm.Slug.Trim().ToLower();
            product.Description  = vm.Description;
            product.BasePrice    = vm.BasePrice;
            product.SalePrice    = vm.SalePrice;
            product.ThumbnailUrl = vm.ThumbnailUrl;
            product.Stock        = vm.Stock;
            product.IsActive     = vm.IsActive;
            product.IsFeatured   = vm.IsFeatured;
            product.UpdatedAt    = DateTime.UtcNow;

            // Sync category
            _context.ProductCategories.RemoveRange(product.ProductCategories);
            if (vm.CategoryId.HasValue)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId  = id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            // Sync attribute values (remove-all then re-insert avoids unique-index conflicts)
            _context.ProductAttributeValues.RemoveRange(product.ProductAttributeValues);
            foreach (var avId in vm.SelectedAttributeValueIds.Where(x => x > 0).Distinct())
            {
                _context.ProductAttributeValues.Add(new ProductAttributeValue
                {
                    ProductId        = id,
                    AttributeValueId = avId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được cập nhật!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // GET: Admin/Product/Delete/5
        // =====================================================================
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // =====================================================================
        // POST: Admin/Product/Delete/5
        // =====================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được xoá!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // Private helpers
        // =====================================================================

        /// <summary>
        /// Loads attribute groups for <paramref name="categoryId"/> PLUS all its
        /// ancestor categories (union), so attributes defined on parent categories
        /// are inherited by child products.  De-duplication: if the same Attribute
        /// appears on both leaf and ancestor, the leaf-level mapping wins.
        /// </summary>
        private async Task<List<AttributeGroupVM>> LoadAttributeGroups(int categoryId)
        {
            var allCats = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();

            var ancestorIds = GetAncestorIds(allCats, categoryId);

            var catAttrs = await _context.CategoryAttributes
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

            // De-duplicate: closest to leaf takes priority
            var unique = catAttrs
                .GroupBy(ca => ca.AttributeId)
                .Select(g => g.OrderBy(ca => ancestorIds.IndexOf(ca.CategoryId)).First())
                .OrderBy(ca => ca.SortOrder)
                .ThenBy(ca => ca.Attribute.DisplayName)
                .ToList();

            return unique.Select(ca => new AttributeGroupVM
            {
                AttributeId = ca.AttributeId,
                Name        = ca.Attribute.Name,
                DisplayName = ca.Attribute.DisplayName,
                SortOrder   = ca.SortOrder,
                Options     = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.DisplayName)
                    .Select(v => new AttributeValueOptionVM { Id = v.Id, DisplayName = v.DisplayName })
                    .ToList()
            }).ToList();
        }

        /// <summary>
        /// All attribute groups that have at least one CategoryAttribute mapping —
        /// used for the Index filter panel.
        /// </summary>
        private async Task<List<AttributeGroupVM>> LoadFilterAttributeGroups()
        {
            var attrs = await _context.Attributes
                .Include(a => a.Values)
                .Include(a => a.CategoryAttributes)
                .Where(a => a.CategoryAttributes.Any())
                .OrderBy(a => a.SortOrder)
                .ToListAsync();

            return attrs.Select(a => new AttributeGroupVM
            {
                AttributeId = a.Id,
                Name        = a.Name,
                DisplayName = a.DisplayName,
                SortOrder   = a.SortOrder,
                Options     = a.Values
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new AttributeValueOptionVM { Id = v.Id, DisplayName = v.DisplayName })
                    .ToList()
            }).ToList();
        }

        /// <summary>
        /// Returns [leaf_id, parent_id, …, root_id] for the given category.
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
        /// Validates that every submitted AttributeValueId belongs to an attribute
        /// that is applicable to the product's chosen category (+ its ancestors).
        /// Adds a ModelState error if any value is out of scope.
        /// </summary>
        private async Task ValidateAttributeValues(ProductFormVM vm)
        {
            var allCats     = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            var ancestorIds = GetAncestorIds(allCats, vm.CategoryId!.Value);

            var applicableAttrIds = await _context.CategoryAttributes
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Select(ca => ca.AttributeId)
                .Distinct()
                .ToListAsync();

            var validValueIds = (await _context.AttributeValues
                .Where(v => applicableAttrIds.Contains(v.AttributeId))
                .Select(v => v.Id)
                .ToListAsync()).ToHashSet();

            var submitted = vm.SelectedAttributeValueIds.Where(id => id > 0).ToList();
            if (submitted.Any(id => !validValueIds.Contains(id)))
            {
                ModelState.AddModelError("SelectedAttributeValueIds",
                    "Một số giá trị thuộc tính không thuộc danh mục đã chọn.");
            }
        }
    }
}
