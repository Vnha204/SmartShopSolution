using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShop.Data.Repository.IRepository;
using SmartShop.Models;
using SmartShop.Utility;

namespace SmartShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/api/categories")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CategoryApiController> _logger;

        public CategoryApiController(IUnitOfWork unitOfWork, ILogger<CategoryApiController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        //Admin/api/categories/children?parentId=
        [HttpGet("children")]
        public async Task<IActionResult> GetChildren([FromQuery] int? parentId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var children = allCats
                .Where(c => c.ParentId == parentId)
                .Select(c => new
                {
                    id          = c.Id,
                    name        = c.Name,
                    slug        = c.Slug,
                    hasChildren = allCats.Any(ch => ch.ParentId == c.Id)
                })
                .ToList();

            return Json(children);
        }

        //Admin/api/categories/breadcrumb?categoryId=
        [HttpGet("breadcrumb")]
        public async Task<IActionResult> GetBreadcrumb([FromQuery] int categoryId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();
            var breadcrumb = BuildBreadcrumb(allCats, categoryId);
            return Json(breadcrumb.Select(c => new { id = c.Id, name = c.Name, slug = c.Slug }));
        }

        //Admin/api/categories/{categoryId}/attributes?productId=
        [HttpGet("{categoryId}/attributes")]
        public async Task<IActionResult> GetAttributes(int categoryId, [FromQuery] int? productId)
        {
            var allCats = await _unitOfWork.Categories.Query(c => c.IsActive).ToListAsync();
            var ancestorIds = GetAncestorIds(allCats, categoryId);
            if (!ancestorIds.Any())
                return Json(Array.Empty<object>());

            var catAttrs = await _unitOfWork.CategoryAttributes.Query()
                .Where(ca => ancestorIds.Contains(ca.CategoryId))
                .Include(ca => ca.Attribute)
                    .ThenInclude(a => a.Values)
                .ToListAsync();

            var uniqueAttrs = catAttrs
                .GroupBy(ca => ca.AttributeId)
                .Select(g => g.OrderBy(ca => ancestorIds.IndexOf(ca.CategoryId)).First())
                .OrderBy(ca => ca.SortOrder)
                .ThenBy(ca => ca.Attribute.DisplayName)
                .ToList();

            var selectedValueIds = new HashSet<int>();
            if (productId.HasValue)
            {
                selectedValueIds = (await _unitOfWork.ProductAttributeValues.Query()
                    .Where(pav => pav.ProductId == productId.Value)
                    .Select(pav => pav.AttributeValueId)
                    .ToListAsync())
                    .ToHashSet();
            }

            var result = uniqueAttrs.Select(ca => new
            {
                attributeId = ca.AttributeId,
                displayName = ca.Attribute.DisplayName,
                values = ca.Attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.DisplayName)
                    .Select(v => new
                    {
                        id          = v.Id,
                        value       = v.Value,
                        displayName = v.DisplayName,
                        selected    = selectedValueIds.Contains(v.Id)
                    })
                    .ToList()
            }).ToList();

            return Json(result);
        }

        private static List<int> GetAncestorIds(List<Category> allCats, int categoryId)
        {
            var ids = new List<int>();
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

        private static List<Category> BuildBreadcrumb(List<Category> allCats, int categoryId)
        {
            var chain = new List<Category>();
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
    }
}
