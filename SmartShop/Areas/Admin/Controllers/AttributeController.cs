using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Utility;

using ModelAttribute = SmartShop.Models.Attribute;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class AttributeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AttributeController> _logger;

        public AttributeController(IUnitOfWork unitOfWork, ILogger<AttributeController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Admin/Attribute
        public async Task<IActionResult> Index()
        {
            var attrs = await _unitOfWork.Attributes.Query()
                .Include(a => a.Values)
                .Include(a => a.CategoryAttributes)
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .ToListAsync();
            return View(attrs);
        }

        // GET: Admin/Attribute/Create
        public IActionResult Create() => View(new ModelAttribute());

        // POST: Admin/Attribute/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ModelAttribute model)
        {
            if (await _unitOfWork.Attributes.AnyAsync(a => a.Name == model.Name))
                ModelState.AddModelError("Name", "Tên (Name) thuộc tính đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            await _unitOfWork.Attributes.AddAsync(model);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Attribute created: {Name}", model.DisplayName);
            TempData["Success"] = $"Thuộc tính \"{model.DisplayName}\" đã được thêm.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Attribute/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var attr = await _unitOfWork.Attributes.GetByIdAsync(id);
            if (attr == null) return NotFound();
            return View(attr);
        }

        // POST: Admin/Attribute/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ModelAttribute model)
        {
            if (id != model.Id) return BadRequest();

            if (await _unitOfWork.Attributes.AnyAsync(a => a.Name == model.Name && a.Id != id))
                ModelState.AddModelError("Name", "Tên (Name) thuộc tính đã tồn tại.");

            if (!ModelState.IsValid) return View(model);

            _unitOfWork.Attributes.Update(model);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Attribute updated: {Name} (Id={Id})", model.DisplayName, id);
            TempData["Success"] = $"Đã cập nhật thuộc tính \"{model.DisplayName}\".";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Attribute/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var attr = await _unitOfWork.Attributes.Query()
                .Include(a => a.Values)
                    .ThenInclude(v => v.ProductAttributeValues)
                .Include(a => a.CategoryAttributes)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attr == null) return NotFound();
            return View(attr);
        }

        // POST: Admin/Attribute/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attr = await _unitOfWork.Attributes.Query()
                .Include(a => a.Values)
                    .ThenInclude(v => v.ProductAttributeValues)
                .Include(a => a.CategoryAttributes)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attr == null) return NotFound();

            _unitOfWork.Attributes.Remove(attr);
            await _unitOfWork.SaveAsync();
            _logger.LogInformation("Attribute deleted: Id={Id}", id);
            TempData["Success"] = "Đã xoá thuộc tính.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Attribute/Values/5
        public async Task<IActionResult> Values(int id)
        {
            var attr = await _unitOfWork.Attributes.Query()
                .Include(a => a.Values)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attr == null) return NotFound();
            return View(attr);
        }

        // POST: Admin/Attribute/AddValue
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddValue(int attributeId, string value,
            string displayName, int sortOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(displayName))
            {
                TempData["Error"] = "Giá trị (Value) và tên hiển thị không được để trống.";
                return RedirectToAction(nameof(Values), new { id = attributeId });
            }

            if (await _unitOfWork.AttributeValues.AnyAsync(
                    v => v.AttributeId == attributeId && v.Value == value.Trim()))
            {
                TempData["Error"] = $"Giá trị \"{value}\" đã tồn tại trong thuộc tính này.";
                return RedirectToAction(nameof(Values), new { id = attributeId });
            }

            await _unitOfWork.AttributeValues.AddAsync(new AttributeValue
            {
                AttributeId = attributeId,
                Value       = value.Trim(),
                DisplayName = displayName.Trim(),
                SortOrder   = sortOrder
            });
            await _unitOfWork.SaveAsync();
            TempData["Success"] = $"Đã thêm giá trị \"{displayName}\".";
            return RedirectToAction(nameof(Values), new { id = attributeId });
        }

        // POST: Admin/Attribute/EditValue
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditValue(int id, string value,
            string displayName, int sortOrder = 0)
        {
            var val = await _unitOfWork.AttributeValues.GetByIdAsync(id);
            if (val == null) return NotFound();

            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(displayName))
            {
                TempData["Error"] = "Giá trị và tên hiển thị không được để trống.";
                return RedirectToAction(nameof(Values), new { id = val.AttributeId });
            }

            val.Value       = value.Trim();
            val.DisplayName = displayName.Trim();
            val.SortOrder   = sortOrder;
            await _unitOfWork.SaveAsync();
            TempData["Success"] = $"Đã cập nhật giá trị \"{displayName}\".";
            return RedirectToAction(nameof(Values), new { id = val.AttributeId });
        }

        // POST: Admin/Attribute/DeleteValue
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteValue(int id)
        {
            var val = await _unitOfWork.AttributeValues.Query()
                .Include(v => v.ProductAttributeValues)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (val == null) return NotFound();

            var attributeId = val.AttributeId;
            _unitOfWork.AttributeValues.Remove(val);
            await _unitOfWork.SaveAsync();
            TempData["Success"] = "Đã xoá giá trị thuộc tính.";
            return RedirectToAction(nameof(Values), new { id = attributeId });
        }
    }
}
