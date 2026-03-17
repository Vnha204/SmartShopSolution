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

            // Name search
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = query.Where(p => p.Name.Contains(filter.SearchTerm));

            // Status filter
            if (filter.StatusFilter == "active")
                query = query.Where(p => p.IsActive);
            else if (filter.StatusFilter == "inactive")
                query = query.Where(p => !p.IsActive);

            // Category filter
            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == filter.CategoryId.Value));

            // Price range
            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);

            // Attribute value filter — every selected value must appear on the product
            var activeAttrIds = filter.AttributeValueIds.Where(id => id > 0).ToList();
            foreach (var avId in activeAttrIds)
                query = query.Where(p => p.ProductAttributeValues.Any(pav => pav.AttributeValueId == avId));

            filter.Products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            // Populate filter-UI data
            filter.AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            filter.FilterAttributeGroups = await LoadFilterAttributeGroups();

            return View(filter);
        }

        // =====================================================================
        // AJAX — GET /Admin/Product/GetAttributesByCategory?categoryId=X
        // Returns JSON array of attribute groups for JS to render dynamically.
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetAttributesByCategory(int categoryId)
        {
            var catAttributes = await _context.CategoryAttributes
                .Where(ca => ca.CategoryId == categoryId)
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .OrderBy(ca => ca.SortOrder)
                .ToListAsync();

            var result = catAttributes.Select(ca => new
            {
                attributeId = ca.AttributeId,
                name        = ca.Attribute.Name,
                displayName = ca.Attribute.DisplayName,
                sortOrder   = ca.SortOrder,
                options     = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new { id = v.Id, displayName = v.DisplayName })
                    .ToList()
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
        public async Task<IActionResult> Create()
        {
            var vm = new ProductFormVM
            {
                AllCategories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync()
            };
            return View(vm);
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

            if (!ModelState.IsValid)
            {
                vm.AllCategories = await _context.Categories
                    .Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
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

            // Assign the selected category
            if (vm.CategoryId.HasValue)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId  = product.Id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            // Assign attribute values (skip empty / zero from unselected dropdowns)
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

            // Primary category drives attribute loading (use first assigned category)
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
                    .Select(pav => pav.AttributeValueId).ToList(),
                AllCategories = await _context.Categories
                    .Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
            };

            // Pre-load attributes so they render server-side (no AJAX required on first load)
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

            if (!ModelState.IsValid)
            {
                vm.AllCategories = await _context.Categories
                    .Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                return View(vm);
            }

            var product = await _context.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.ProductAttributeValues)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Update scalar fields
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

            // ── Sync category (replace all with single selected) ──────────────
            _context.ProductCategories.RemoveRange(product.ProductCategories);
            if (vm.CategoryId.HasValue)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId  = id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            // ── Sync attribute values (remove-all then re-insert) ─────────────
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
        /// Loads attribute groups (with options) for a specific category, ordered by SortOrder.
        /// </summary>
        private async Task<List<AttributeGroupVM>> LoadAttributeGroups(int categoryId)
        {
            var catAttributes = await _context.CategoryAttributes
                .Where(ca => ca.CategoryId == categoryId)
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .OrderBy(ca => ca.SortOrder)
                .ToListAsync();

            return catAttributes.Select(ca => new AttributeGroupVM
            {
                AttributeId = ca.AttributeId,
                Name        = ca.Attribute.Name,
                DisplayName = ca.Attribute.DisplayName,
                SortOrder   = ca.SortOrder,
                Options     = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new AttributeValueOptionVM { Id = v.Id, DisplayName = v.DisplayName })
                    .ToList()
            }).ToList();
        }

        /// <summary>
        /// Loads all attribute groups that have at least one CategoryAttribute mapping.
        /// Used to populate the filter panel on the Index page.
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
    }
}
