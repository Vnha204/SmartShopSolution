using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Data;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryAttributeController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryAttributeController(AppDbContext context) => _context = context;

        // =====================================================================
        // GET: Admin/CategoryAttribute
        // Lists every active category with its mapped-attribute count
        // =====================================================================
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.CategoryAttributes)
                .Include(c => c.Parent)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentId == null ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // =====================================================================
        // GET: Admin/CategoryAttribute/Manage/5
        // Edit which Attributes are mapped to category {id}
        // =====================================================================
        public async Task<IActionResult> Manage(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var allAttributes = await _context.Attributes
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.DisplayName)
                .ToListAsync();

            var mappedIds = (await _context.CategoryAttributes
                .Where(ca => ca.CategoryId == id)
                .Select(ca => ca.AttributeId)
                .ToListAsync()).ToHashSet();

            ViewBag.Category     = category;
            ViewBag.AllAttributes = allAttributes;
            ViewBag.MappedIds    = mappedIds;
            return View();
        }

        // =====================================================================
        // POST: Admin/CategoryAttribute/Manage/5
        // =====================================================================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int id, List<int>? enabledAttributeIds)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var existing = await _context.CategoryAttributes
                .Where(ca => ca.CategoryId == id)
                .ToListAsync();

            var existingIds = existing.Select(ca => ca.AttributeId).ToHashSet();
            var newIds      = (enabledAttributeIds ?? new List<int>()).ToHashSet();

            // Remove unchecked
            var toRemove = existing.Where(ca => !newIds.Contains(ca.AttributeId)).ToList();
            _context.CategoryAttributes.RemoveRange(toRemove);

            // Add newly checked
            foreach (var attrId in newIds.Except(existingIds))
            {
                _context.CategoryAttributes.Add(new CategoryAttribute
                {
                    CategoryId  = id,
                    AttributeId = attrId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật ánh xạ thuộc tính cho danh mục \"{category.Name}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
