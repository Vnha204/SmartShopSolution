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
    public class CategoryAttributeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryAttributeController> _logger;

        public CategoryAttributeController(IUnitOfWork unitOfWork, ILogger<CategoryAttributeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Admin/CategoryAttribute
        public async Task<IActionResult> Index()
        {
            var categories = await _unitOfWork.Categories.Query()
                .Include(c => c.CategoryAttributes)
                .Include(c => c.Parent)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: Admin/CategoryAttribute/Manage/5
        public async Task<IActionResult> Manage(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return NotFound();

            var allAttributes = await _unitOfWork.Attributes.Query()
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.DisplayName)
                .ToListAsync();

            var mappedIds = (await _unitOfWork.CategoryAttributes.Query()
                .Where(ca => ca.CategoryId == id)
                .Select(ca => ca.AttributeId)
                .ToListAsync()).ToHashSet();

            ViewBag.Category      = category;
            ViewBag.AllAttributes = allAttributes;
            ViewBag.MappedIds     = mappedIds;
            return View();
        }

        // POST: Admin/CategoryAttribute/Manage/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int id, List<int>? enabledAttributeIds)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return NotFound();

            var existing    = (await _unitOfWork.CategoryAttributes.FindAsync(ca => ca.CategoryId == id)).ToList();
            var existingIds = existing.Select(ca => ca.AttributeId).ToHashSet();
            var newIds      = (enabledAttributeIds ?? new List<int>()).ToHashSet();

            var toRemove = existing.Where(ca => !newIds.Contains(ca.AttributeId)).ToList();
            _unitOfWork.CategoryAttributes.RemoveRange(toRemove);

            foreach (var attrId in newIds.Except(existingIds))
            {
                await _unitOfWork.CategoryAttributes.AddAsync(new CategoryAttribute
                {
                    CategoryId  = id,
                    AttributeId = attrId
                });
            }

            await _unitOfWork.SaveAsync();
            _logger.LogInformation("CategoryAttribute mapping updated for category Id={Id}", id);
            TempData["Success"] = $"Đã cập nhật ánh xạ thuộc tính cho danh mục \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
