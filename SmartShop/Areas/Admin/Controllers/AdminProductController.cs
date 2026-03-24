using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Models.ViewModels;
using SmartShop.Services;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinary;
        private readonly ILogger<ProductController> _logger;

        private static readonly HashSet<string> _allowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> _allowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private const long _maxFileSize = 5 * 1024 * 1024; // 5 MB

        public ProductController(IUnitOfWork unitOfWork, ICloudinaryService cloudinary,
            ILogger<ProductController> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        // =====================================================================
        // GET: Admin/Product
        // =====================================================================
        public async Task<IActionResult> Index(ProductIndexVM filter)
        {
            IQueryable<Product> query = _unitOfWork.Products.Query()
                .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                .Include(p => p.ProductAttributeValues).ThenInclude(pav => pav.AttributeValue);

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

            filter.AllCategories = await _unitOfWork.Categories.Query(c => c.IsActive)
                .OrderBy(c => c.Name).ToListAsync();

            filter.FilterAttributeGroups = await LoadFilterAttributeGroups();

            return View(filter);
        }

        // =====================================================================
        // AJAX — GET /Admin/Product/GetAttributesByCategory?categoryId=X
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
            var product = await _unitOfWork.Products.GetWithFullDetailsAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // =====================================================================
        // GET: Admin/Product/Create
        // =====================================================================
        public IActionResult Create()
        {
            return View(new ProductFormVM());
        }

        // =====================================================================
        // POST: Admin/Product/Create
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormVM vm)
        {
            if (vm.ThumbnailFile == null || vm.ThumbnailFile.Length == 0)
                ModelState.AddModelError("ThumbnailFile", "Vui lòng chọn ảnh sản phẩm.");
            else
                ValidateThumbnailFile(vm.ThumbnailFile);

            if (await _unitOfWork.Products.AnyAsync(p => p.Slug == vm.Slug))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            if (vm.CategoryId.HasValue)
            {
                if (await _unitOfWork.Categories.AnyAsync(c => c.ParentId == vm.CategoryId.Value && c.IsActive))
                    ModelState.AddModelError("CategoryId",
                        "Vui lòng chọn danh mục cấp cuối (danh mục không có danh mục con).");
            }

            if (vm.CategoryId.HasValue && vm.SelectedAttributeValueIds.Any(id => id > 0))
                await ValidateAttributeValues(vm);

            if (!ModelState.IsValid)
            {
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                return View(vm);
            }

            string thumbnailUrl      = string.Empty;
            string thumbnailPublicId = string.Empty;
            try
            {
                (thumbnailUrl, thumbnailPublicId) =
                    await _cloudinary.UploadImageAsync(vm.ThumbnailFile!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed during product create");
                ModelState.AddModelError("ThumbnailFile", $"Upload ảnh thất bại: {ex.Message}");
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                return View(vm);
            }

            var product = new Product
            {
                Name              = vm.Name,
                Slug              = vm.Slug.Trim().ToLower(),
                Description       = vm.Description,
                BasePrice         = vm.BasePrice,
                SalePrice         = vm.SalePrice,
                ThumbnailUrl      = thumbnailUrl,
                ThumbnailPublicId = thumbnailPublicId,
                Stock             = vm.Stock,
                IsActive          = vm.IsActive,
                IsFeatured        = vm.IsFeatured,
                CreatedAt         = DateTime.UtcNow,
                UpdatedAt         = DateTime.UtcNow
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveAsync();

            if (vm.CategoryId.HasValue)
            {
                await _unitOfWork.ProductCategories.AddAsync(new ProductCategory
                {
                    ProductId  = product.Id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            foreach (var avId in vm.SelectedAttributeValueIds.Where(id => id > 0).Distinct())
            {
                await _unitOfWork.ProductAttributeValues.AddAsync(new ProductAttributeValue
                {
                    ProductId        = product.Id,
                    AttributeValueId = avId
                });
            }

            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Product created: {Name} (Id={Id})", product.Name, product.Id);
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được thêm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // GET: Admin/Product/Edit/5
        // =====================================================================
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _unitOfWork.Products.GetWithCategoriesAndAttributesAsync(id);
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

            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
                ValidateThumbnailFile(vm.ThumbnailFile);

            if (await _unitOfWork.Products.AnyAsync(p => p.Slug == vm.Slug && p.Id != id))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            if (vm.CategoryId.HasValue)
            {
                if (await _unitOfWork.Categories.AnyAsync(c => c.ParentId == vm.CategoryId.Value && c.IsActive))
                    ModelState.AddModelError("CategoryId",
                        "Vui lòng chọn danh mục cấp cuối (danh mục không có danh mục con).");
            }

            if (vm.CategoryId.HasValue && vm.SelectedAttributeValueIds.Any(id2 => id2 > 0))
                await ValidateAttributeValues(vm);

            if (!ModelState.IsValid)
            {
                if (vm.CategoryId.HasValue)
                    vm.AvailableAttributes = await LoadAttributeGroups(vm.CategoryId.Value);
                var existing = await _unitOfWork.Products.GetByIdAsync(id);
                if (existing != null) vm.ThumbnailUrl = existing.ThumbnailUrl;
                return View(vm);
            }

            var product = await _unitOfWork.Products.GetWithCategoriesAndAttributesAsync(id);
            if (product == null) return NotFound();

            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
            {
                try
                {
                    await _cloudinary.DeleteImageAsync(product.ThumbnailPublicId);
                    var (newUrl, newPublicId) = await _cloudinary.UploadImageAsync(vm.ThumbnailFile);
                    product.ThumbnailUrl      = newUrl;
                    product.ThumbnailPublicId = newPublicId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Image upload failed during product edit id={Id}", id);
                    ModelState.AddModelError("ThumbnailFile", $"Upload ảnh thất bại: {ex.Message}");
                    vm.AvailableAttributes = vm.CategoryId.HasValue
                        ? await LoadAttributeGroups(vm.CategoryId.Value)
                        : new List<AttributeGroupVM>();
                    vm.ThumbnailUrl = product.ThumbnailUrl;
                    return View(vm);
                }
            }

            product.Name        = vm.Name;
            product.Slug        = vm.Slug.Trim().ToLower();
            product.Description = vm.Description;
            product.BasePrice   = vm.BasePrice;
            product.SalePrice   = vm.SalePrice;
            product.Stock       = vm.Stock;
            product.IsActive    = vm.IsActive;
            product.IsFeatured  = vm.IsFeatured;
            product.UpdatedAt   = DateTime.UtcNow;

            _unitOfWork.ProductCategories.RemoveRange(product.ProductCategories);
            if (vm.CategoryId.HasValue)
            {
                await _unitOfWork.ProductCategories.AddAsync(new ProductCategory
                {
                    ProductId  = id,
                    CategoryId = vm.CategoryId.Value
                });
            }

            _unitOfWork.ProductAttributeValues.RemoveRange(product.ProductAttributeValues);
            foreach (var avId in vm.SelectedAttributeValueIds.Where(x => x > 0).Distinct())
            {
                await _unitOfWork.ProductAttributeValues.AddAsync(new ProductAttributeValue
                {
                    ProductId        = id,
                    AttributeValueId = avId
                });
            }

            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Product updated: {Name} (Id={Id})", product.Name, product.Id);
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được cập nhật!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // GET: Admin/Product/Delete/5
        // =====================================================================
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _unitOfWork.Products.Query()
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
            var product = await _unitOfWork.Products.GetWithCategoriesAndAttributesAsync(id);
            if (product == null) return NotFound();

            try
            {
                await _cloudinary.DeleteImageAsync(product.ThumbnailPublicId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cloudinary delete failed for product id={Id}, continuing", id);
            }

            _unitOfWork.Products.Remove(product);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Product deleted: {Name} (Id={Id})", product.Name, product.Id);
            TempData["Success"] = $"Sản phẩm \"{product.Name}\" đã được xoá!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // Private helpers
        // =====================================================================

        private async Task<List<AttributeGroupVM>> LoadAttributeGroups(int categoryId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();
            var ancestorIds = GetAncestorIds(allCats, categoryId);

            var catAttrs = await _unitOfWork.CategoryAttributes.Query()
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

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

        private async Task<List<AttributeGroupVM>> LoadFilterAttributeGroups()
        {
            var attrs = await _unitOfWork.Attributes.Query()
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

        private void ValidateThumbnailFile(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!_allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError("ThumbnailFile",
                    "Định dạng ảnh không hợp lệ. Chỉ chấp nhận: JPG, PNG, WebP.");
                return;
            }
            if (!_allowedContentTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError("ThumbnailFile", "Loại tệp không được hỗ trợ.");
                return;
            }
            if (file.Length > _maxFileSize)
                ModelState.AddModelError("ThumbnailFile", "Ảnh không được vượt quá 5 MB.");
        }

        private async Task ValidateAttributeValues(ProductFormVM vm)
        {
            var allCats     = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();
            var ancestorIds = GetAncestorIds(allCats, vm.CategoryId!.Value);

            var applicableAttrIds = await _unitOfWork.CategoryAttributes.Query()
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Select(ca => ca.AttributeId)
                .Distinct()
                .ToListAsync();

            var validValueIds = (await _unitOfWork.AttributeValues.Query()
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
