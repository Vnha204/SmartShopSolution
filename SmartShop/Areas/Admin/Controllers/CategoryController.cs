using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IUnitOfWork unitOfWork, ILogger<CategoryController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        //Admin/Category
        public async Task<IActionResult> Index()
        {
            var categories = await _unitOfWork.Categories.Query()
                .Include(c => c.Parent)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();
            return View(categories);
        }

        //Admin/Category/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await _unitOfWork.Categories
                .Query(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(new Category());
        }

        //Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (await _unitOfWork.Categories.AnyAsync(c => c.Slug == model.Slug))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            if (!ModelState.IsValid)
            {
                ViewBag.ParentCategories = await _unitOfWork.Categories
                    .Query(c => c.IsActive && c.ParentId == null).OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            model.Slug = model.Slug.Trim().ToLower();
            model.CreatedAt = TimeHelper.VietnamNow();

            await _unitOfWork.Categories.AddAsync(model);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Category created: {Name}", model.Name);
            TempData["Success"] = $"Danh mục \"{model.Name}\" đã được thêm thành công!";
            return RedirectToAction(nameof(Index));
        }

        //Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return NotFound();

            ViewBag.ParentCategories = await _unitOfWork.Categories
                .Query(c => c.IsActive && c.ParentId == null && c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(category);
        }

        //Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return BadRequest();

            if (await _unitOfWork.Categories.AnyAsync(c => c.Slug == model.Slug && c.Id != id))
                ModelState.AddModelError("Slug", "Slug này đã tồn tại, vui lòng chọn slug khác.");

            if (model.ParentId == id)
                ModelState.AddModelError("ParentId", "Danh mục không thể là cha của chính nó.");

            if (!ModelState.IsValid)
            {
                ViewBag.ParentCategories = await _unitOfWork.Categories
                    .Query(c => c.IsActive && c.ParentId == null && c.Id != id)
                    .OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return NotFound();

            category.Name      = model.Name;
            category.Slug      = model.Slug.Trim().ToLower();
            category.ParentId  = model.ParentId;
            category.ImageUrl  = model.ImageUrl;
            category.SortOrder = model.SortOrder;
            category.IsActive  = model.IsActive;

            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Category updated: {Name} (Id={Id})", category.Name, id);
            TempData["Success"] = $"Danh mục \"{category.Name}\" đã được cập nhật!";
            return RedirectToAction(nameof(Index));
        }

        //Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _unitOfWork.Categories.Query()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();
            return View(category);
        }

        //Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _unitOfWork.Categories.Query()
                .Include(c => c.Children)
                .Include(c => c.ProductCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            if (category.Children.Any())
            {
                TempData["Error"] = "Không thể xoá danh mục có danh mục con. Hãy xoá hoặc chuyển danh mục con trước.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.Categories.Remove(category);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("Category deleted: {Name} (Id={Id})", category.Name, id);
            TempData["Success"] = $"Danh mục \"{category.Name}\" đã được xoá!";
            return RedirectToAction(nameof(Index));
        }
    }
}
